using AngleSharp;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using ClosedXML.Excel;
using CsvHelper.Excel;
using System;
using Parser.UI;
using Common.Logging;
using System.Collections.Generic;
using System.Linq;
using Parser.Pages;
using System.Net;
using Parser.UI.Properties;

// ReSharper disable once CheckNamespace
namespace Parser.DataModel
{
    sealed class MainViewModel : BaseViewModel
    {
        #region Fields

        ///private readonly IBrowsingContext _context;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MainViewModel));
        private CancellationTokenSource _cts;
        private Task _current;
        private string _status;
        private bool _isHarvest = true;
        private bool _isAddressEnable = true;
        private bool _isHarvestEnable = true; 
        private string elementText = "Старт"; 
        private int _pages = 100000;
        private int _page; 
        private int _sitesFound;
        private string _sessionFileName = string.Empty;


        private string _address = string.Empty;
        //private string _address = "1.xlsx";
		//private string _address = "http://1click.ru";
		//private string _address = "https://yandex.ru/yaca/geo/Russia/synt2/Goods_and_Services/";


		#endregion

		#region Child View Models

		private readonly ProcessViewModel _process;
        private readonly UserAgent _userAgent;
        private readonly Proxies _proxy;

        #endregion

        #region ctor

        public MainViewModel()
        {
            _userAgent = new UserAgent("useragents.xml");
            _proxy = new Proxies("proxies.csv");
            _process = new ProcessViewModel(null);
            _cts = new CancellationTokenSource();
        }

        #endregion

        #region Properties
        public ProcessViewModel Process
        {
            get { return _process; }
        }

        public string Address
        {
            get { return _address; }
            set 
            {
                if (value.IsCatalog())
                {
                    var _value = value.ToNormalRef();
                    if (_value.IsMailRu())
                    {
                        _address = _value.RemovePage();
                        _page = _value.GetStartPage();
                    }
                    else
                    {
                        _address = _value;
                        _page = 0;
                    }
                }
                else
                {
                    _address = value;
                    _page = 0;
                }               
                IsHarvest = IsHarvestEnable = _address.IsCatalog();
                RaisePropertyChanged();
            }
        }

        public bool IsHarvestEnable
        {
            get { return _isHarvestEnable; }
            set
            {
                _isHarvestEnable = value;
                RaisePropertyChanged();
            }
        }

        public bool IsAddressEnable
        {
            get { return _isAddressEnable; }
            set
            {
                _isAddressEnable = value;
                RaisePropertyChanged();
            }
        }

        public bool IsHarvest
        {
            get { return _isHarvest; }
            set
            {
                _isHarvest = value;
                RaisePropertyChanged();
            }
        }
        
        public int Page
        {
            get { return _page; }
            set
            {
                _page = value;
                RaisePropertyChanged();
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                RaisePropertyChanged();
            }
        }

        public string ElementText
        {
            get { return elementText; }
            set
            {
                elementText = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Methods

        public void SaveToExcel(string path)
        {
            try
            {
                using (var workbook = new XLWorkbook(XLEventTracking.Disabled))
                {
                    var worksheet = workbook.AddWorksheet("Каталог");
                    using (var writer = new CsvWriter(new ExcelSerializer(worksheet)))
                    {
                        if (_isHarvest)
                        {
                            ResultItemHarvestMap.initConfiguration(writer.Configuration);
                        }
                        else
                        {
                            ResultItemMap.initConfiguration(writer.Configuration);
                        }
                        var result = new List<ResultItem>(_process.Result.ToArray()).ToList();
                        if (Settings.Default.RemoveDublicate)
                        {
                            result = result.Distinct(new ResultItemComparer()).ToList();
                        }
                        writer.WriteRecords(result);
                    }
                    workbook.SaveAs(path);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                new ExceptionViewer("SaveToExcel", ex).ShowDialog();
            }
        }

        public void AutoSaveToExcel(string catalogName = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(catalogName))
                {
                    catalogName = "Catalog";
                }
                if (string.IsNullOrWhiteSpace(_sessionFileName))
                {
                    _sessionFileName = $"{catalogName.VaildFileName()}-{DateTime.Now:dd-MM-yyyy HH-mm-ss}";
                }
                using (var workbook = new XLWorkbook(XLEventTracking.Disabled))
                {
                    var worksheet = workbook.AddWorksheet("Каталог");
                    using (var writer = new CsvWriter(new ExcelSerializer(worksheet)))
                    {
                        if (_isHarvest)
                        {
                            ResultItemHarvestMap.initConfiguration(writer.Configuration);
                        }
                        else
                        {
                            ResultItemMap.initConfiguration(writer.Configuration);
                        }

                        var result = new List<ResultItem>(_process.Result.ToArray()).ToList();
                        if (Settings.Default.RemoveDublicate)
                        {
                            result = result.Distinct(new ResultItemComparer()).ToList();
                        }
                        writer.WriteRecords(result);
                    }
                    var path = Path.Combine(Environment.CurrentDirectory, $"{_sessionFileName}.xlsx");
                    if (File.Exists(path))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch
                        {
                            _sessionFileName = $"{catalogName.VaildFileName()}-{DateTime.Now:dd-MM-yyyy HH-mm-ss}";
                            path = Path.Combine(Environment.CurrentDirectory, $"{_sessionFileName}.xlsx");
                        }
                    }
                    workbook.SaveAs(path);
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex);
            }

        }


        public void Go()
        {
            if (_current != null && !_current.IsCompleted)
            {
                if (!_current.IsCanceled)
                {
                    _cts.Cancel();
                }
            }
            else
            {
                _current = null;
                _cts = new CancellationTokenSource();
                _process.ClearData();
                _process.ClearResult(); 
                ElementText = "Стоп";
                _process.Message($"Обработано ресурсов {_sitesFound}");
                if (_address.IsFile())
                {
                    #region Process Files
                    Url[] uriToParse = null;
                    try
                    {
                        Status = $"Загрузка документа {_address}";
                        using (var workbook = new XLWorkbook(_address, XLEventTracking.Disabled))
                        {
                            using (var reader = new CsvReader(new ExcelParser(workbook)))
                            {
                                ResultItemHarvestMap.initConfiguration(reader.Configuration);
                                uriToParse = reader.GetRecords<ResultItem>().Select(_ => new Url(_.Url)).ToArray();
                            }
                        }
                        Status = $"Документа {_address} загружен. Найдено {uriToParse.Count()} ссылок";
                    }
                    catch(Exception ex)
                    {
                        new ExceptionViewer("Неверный формат файла.", ex).ShowDialog();
                    }
                    if (uriToParse != null && uriToParse.Any())
                    {
                        var i = 0;
                        var links = 10;
                        _sitesFound = 0;
                        while (i <= uriToParse.Count()-1)
                        {
                            try
                            {
                                if (_current == null || (_current != null && _current.IsCompleted))
                                {
                                    if (_current != null && _current.IsCanceled)
                                    {
                                        break;
                                    }
                                    var _list = uriToParse.Skip(i).Take(links).ToList();
                                    _current = ProcessUris(_list, _cts.Token);
                                    _sitesFound = _sitesFound + _list.Count();
                                    Status = $"Обработано ресурсов {_sitesFound} ";
                                    AutoSaveToExcel();
                                    i = i + links;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                _process.Message($"Задача отменена, страница {_page}");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                            }
                        }
                    }

                    #endregion Process Files
                }
                else if (_address.IsCatalog())
                {
                    #region Process Catalog
                    while (_page <= _pages)
                    {
                        try
                        {
                            if (_current == null || (_current != null && _current.IsCompleted))
                            {
                                if (_current != null && _current.IsCanceled)
                                {
                                    break;
                                }
                                _process.Message(
	                                $"Загрузка страницы {((_page == 0) ? string.Empty : _page.ToString())}...");
                                var url = Path.Combine(_address, (_page == 0) ? string.Empty :
                                    string.Format(_address.GetPageTemplate(), _page)).CreateUrl();
                                _process.Message($"Ссылка {url}...");
                                _current = LoadAsync(url, _cts.Token);
                                _page++;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            _process.Message($"Задача отменена, страница {_page}");
                        }
                        catch (AggregateException ex)
                        {
                            foreach (var e in ex.InnerExceptions)
                            {
                                if (e is CaptchaRequestException)
                                {
                                    var message = "Запрос подтверждения на использование данных в автоматическом режиме";
                                    _process.Message(message);
                                    Status = message;
                                    Logger.Error(ex);
                                    _current.Wait(new TimeSpan(0, 10, 0));
                                }
                                else
                                {
                                    Logger.Error(ex);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }
                    #endregion Process Catalog
                }
                else
                {
                    ProcessResource();
                }
                Reset();
            }
        }
       
        private void ProcessResource()
        {
            try
            {
                var url = _address.CreateUrl();
                var message = $"Обработка ресурса {url}...";
                _process.Message(message);
                Status = message;
                _process.ProcessUri(url, _cts.Token);
                AutoSaveToExcel(url.HostName);
                message = $"Завершена обработка ресурса {url}...";
                _process.Message(message);
                Status = message;
            }
            catch (OperationCanceledException)
            {
                Reset();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void Reset()
        {
            ElementText = "Старт";
            _sessionFileName = string.Empty;
            _sitesFound = 0;
        }

        private async Task<AngleSharp.Dom.IDocument> _getDocument(Uri uri)
        {
            var success = false;
            var content = string.Empty;
            var tryCount = 1;
            while (!success)
            {
                try
                {
                    Status = string.Format("Найдено ресурсов: {2} Страница: {1} Попытка : {0}", tryCount, uri,_sitesFound);
                    using (var web = new WebClient())
                    {
                        ProxyItem proxy = _proxy.GetItem();
                        if (proxy != null && Settings.Default.UseProxy)
                        {
                            web.Proxy = new WebProxy(proxy.ip, proxy.port);
                        }
                        web.Credentials = CredentialCache.DefaultNetworkCredentials;
                        web.Encoding = System.Text.Encoding.UTF8;
                        web.Headers.Add("User-Agent", _userAgent.GetItem());
                        try
                        {
                            content = web.DownloadString(uri);
                            if (content.Contains("yandex.ru/captcha"))
                            {
                                throw new CaptchaRequestException();
                            }
                            success = true;
                        }
                        catch(WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                            {
                                var resp = (HttpWebResponse)ex.Response;
                                if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                                {
                                    if (uri.ToString().IsMailRu())
                                    {
                                        throw new EndCatalogExeptionException();
                                    }
                                }
                            }
                            throw;
                        }
                    }
                }
                catch(EndCatalogExeptionException)
                {
                    throw;
                }
                catch(Exception ex)
                {
                    tryCount++;
                    Logger.Error(ex);
                }
            }
            return await BrowsingContext.New().OpenAsync(r => r.Address(uri).Content(content));
        }

        private async Task ProcessUris(List<Url> urls, CancellationToken token)
        {
            await Task.Run(() =>
            {
                var taskList = new List<Task>();
                foreach (var item in urls)
                {
                    var LastTask = new Task(() => _process.ProcessUri(item, token));
                    LastTask.Start();
                    taskList.Add(LastTask);
                }
                Task.WaitAll(taskList.ToArray());
            }, token);
        }

        private async Task LoadAsync(Url url, CancellationToken token)
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var document = _getDocument(url).Result;
                        token.ThrowIfCancellationRequested();
                        _process.Message(Status);
                        _process.Document = document;
                        var uriToParse = _process.ProcessCatalog(document);
                        foreach (var item in uriToParse)
                        {
                            _sitesFound++;
                            if (_isHarvest)
                            {
                                var result = new ResultItem { Url = item.ToString() };
                                Status = $"Найдено ресурсов {_sitesFound}";
                                _process.PrcosessMessage(result.Url);
                                while (!_process.Result.TryAdd(result, 1000, token))
                                {
                                    Logger.ErrorFormat("Невозможно добавить элемент {0}", result.Url);
                                }
                            }
                            else
                            {
                                _process.Message($"Обработка страницы {item}...");
                                _process.ProcessUri(item, token);
                                Status = $"Обработано ресурсов {_sitesFound}";
                            }
                            token.ThrowIfCancellationRequested();
                        }
                        AutoSaveToExcel(document.Domain);
                        document.Dispose();
                    }
                    catch (AggregateException ex)
                    {
                        if (ex.InnerException is EndCatalogExeptionException)
                        {
                            throw;
                        }
                        else
                        {
                            Logger.Error(ex.Flatten());
                        }
                    }
                }, token);
            }
            catch(Exception ex)
            {
                if (ex.InnerException is EndCatalogExeptionException)
                {
                    _cts.Cancel();
                }
                else if (ex is TaskCanceledException)
                {
                    throw;
                }
                else
                {
                    Logger.Error(ex);
                }
            }
        }

        #endregion
    }
}