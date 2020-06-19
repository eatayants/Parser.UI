using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using Common.Logging;
using Parser.UI;
using Parser.UI.DataModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;

namespace Parser.DataModel
{
    class ProcessViewModel : BaseViewModel
    {
        //private readonly IBrowsingContext _context;
        public IDocument _document;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProcessViewModel));
        private readonly BlockingCollection<ResultItem> _parseResult;
        private readonly ObservableCollection<ProcessItem> _parseProcess;

        public ProcessViewModel(IBrowsingContext context)
        {
            //_context = context;
            _parseResult = new BlockingCollection<ResultItem>();
            _parseProcess = new ObservableCollection<ProcessItem>();
            Register<CssErrorEvent>(m => App.Current.Dispatcher.Invoke(() => 
                Message(string.Format("Ссылка {1} Ошибка стилей страницы: {0} ", m.Message,((BrowsingContext)m.CurrentTarget).Active.Domain))));
            Register<HtmlErrorEvent>(m => App.Current.Dispatcher.Invoke(() => 
                Message(string.Format("Ссылка {1} Ошибка HTML кода страницы: {0} ", m.Message, ((BrowsingContext)m.CurrentTarget).Active.Domain))));
        }

        public ObservableCollection<ProcessItem> Data
        {
            get { return _parseProcess; }
        }
        
        public BlockingCollection<ResultItem> Result
        {
            get { return _parseResult; }
        }

        public void Message(string item)
        {
           Logger.Info(item);
        }

        public void PrcosessMessage(string url, string title = "", bool found = false)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                title = string.IsNullOrWhiteSpace(title) ? url : title;
                var item = _parseProcess.FirstOrDefault(_ => _.Url == url);
                if (item != null)
                {
                    item.Message = string.Format("Совпадения {0} найдены - {1}", 
                        found ? string.Empty : "не", item.Title);
                }
                else
                {
                    item = new ProcessItem
                    {
                        Title = title.SafeSubstring(0, 100),
                        Message = title.SafeSubstring(0, 100),
                        Tick = DateTime.Now,
                        Url = url
                    };
                    _parseProcess.Add(item);
                }
            });
        }

        private void Register<T>(Action<T> listener) where T : Event
        {
            /*_context.ParseError += (obj, ev) =>
            {
                var data = ev as T;
                if (data != null)
                {
                    listener.Invoke(data);
                }
            };*/
        }

        public IDocument Document
        {
            get
            {
                return _document;
            }
            set
            {
                _document = value;
            }
        }
        public ICollection<Uri> ProcessCatalog(IDocument document)
        {
            ICollection<Uri> result = null;
            var found = 0;
            if (document != null)
            {
                if (document.Url.IsYa())
                {
                    const string selector = "a.yaca-snippet__title-link";
                    var resultcollection = document.QuerySelectorAll(selector)
                            .Select(_ => new Uri(_.GetAttribute("href"))).ToList();
                        result = resultcollection.Where(_ => _.Host != document.Domain).ToList();
                    found = result.Count;
                }
                else if (document.Url.IsMailRu())
                {
                    var resultcollection = document.QuerySelectorAll(".rez-h").ToList();
                    resultcollection.AddRange(document.QuerySelectorAll(".rez-descr"));

                    var uris = new List<Uri>();
                    foreach(var item in resultcollection)
                    {
                        if (item.HasChildNodes)
                        {
                            Uri uri = null;
                            var href = item.FirstElementChild.GetAttribute("href");
                            var textContent = item.FirstElementChild.TextContent;
                            if (!string.IsNullOrWhiteSpace(href) || (!string.IsNullOrWhiteSpace(textContent)))
                            {

                                if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
                                {
                                    Uri.TryCreate(href, UriKind.Absolute, out uri);
                                }
                                else if (Uri.IsWellFormedUriString(textContent, UriKind.Absolute))
                                {
                                    Uri.TryCreate(textContent, UriKind.Absolute, out uri);
                                }
                            }
                            if (uri != null)
                            {
                                uris.Add(uri);
                            }
                        }
                    }
                    result = uris.Distinct(new UrlComparer()).ToArray();
                    found = result.Count;
                }
            }
            Message(string.Format("Найдено {0} сайтов.", result.Count));
            return result;
        }
        internal void ClearResult()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _parseResult.Clear();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        internal void ClearData()
        {
            App.Current.Dispatcher.Invoke(() => _parseProcess.Clear());
        }

        public void ProcessUri(Uri address, CancellationToken cancel)
        {
            try
            {
                var url = address.ToString().CreateUrl();
                if (url != null)
                {
                    using (var parser = new DocumentParser(cancel))
                    {
                        var result = parser.Execute(url);
                        if (result != null)
                        {
                            PrcosessMessage(result.Url, result.Status == HttpStatusCode.OK ? result.Title : result.Url);
                            while (!_parseResult.TryAdd(result, 1000, cancel))
                            {
                                Logger.ErrorFormat("Невозможно добавить элемент", result.Url);
                            }
                        }
                        cancel.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.InfoFormat("Задача отменена {0}", address!=null?address.ToString():string.Empty);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }
    }
}