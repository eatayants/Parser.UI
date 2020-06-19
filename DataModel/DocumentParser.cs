using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Network.Default;
using Parser.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parser.DataModel
{
    public class DocumentParser : IDisposable
    {
        private readonly string[] fileExt = { ".DOC", ".DOCX", ".ODT", ".PDF", ".XLS", ".XLSX", ".ODS", ".PPT", ".PPTX" };
        private IConfiguration _config;
        private readonly CancellationToken _token;
        private UserAgent _userAgent;
        private readonly TimeSpan _timeOut;

        public DocumentParser(CancellationToken token)
        {
            _userAgent = new UserAgent("useragents.xml");
            _timeOut = new TimeSpan(0, 3, 0);
            _config = InitConfig();
            _token = token;
        }

        private IConfiguration InitConfig()
        {
            var userAgent = _userAgent.GetItem();
            var requester = new HttpRequester(userAgent);
            requester.Headers["User-Agent"] = userAgent;
            requester.Timeout = _timeOut;
            return Configuration.Default.WithDefaultLoader(requesters: new[] { requester });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
	        if (!disposing) return;
	        _config = null;
	        _userAgent = null;
        }

        public ResultItem Execute(Url url)
        {
            var result = new ResultItem { Url = url.ToString() };
            var document = GetDocument(url);
            if (document != null)
            {
                result.Url = document.Domain;
                result.Title = document.Title.SafeSubstring(0, 250);
                result.Status = document.StatusCode;
                if (result.Status == System.Net.HttpStatusCode.OK)
                {
                    result.UseCard = GetUseCard(document);
	                ProcessSub(document, result);
					/*if (!result.UseCard && !result.FilledIn())
                    {
                        ProcessSub(document, result);
                    }*/
                    if (result.UseCard && !result.FilledIn())
                    {
	                    if (!result.TempWithoutCard)
	                    {
		                    result.TempWithoutCard = GetTempWithoutCard(document);
	                    }
	                    var email = GetEmail(document);
	                    if (!string.IsNullOrWhiteSpace(email))
	                    {
		                    result.Email = result.Email.MergeSeparatedString(email);
	                    }
	                    var phone = GetPhone(document);
	                    if (!string.IsNullOrWhiteSpace(phone))
	                    {
		                    result.Phone = result.Phone.MergeSeparatedString(phone);
	                    }
	                    if (string.IsNullOrWhiteSpace(result.Inn))
	                    {
		                    result.Inn = GetInn(document);
	                    }
	                    if (string.IsNullOrWhiteSpace(result.RealName))
	                    {
		                    result.RealName = GetRealName(document);
	                    }
					}
                }
                document.Dispose();
            }
            return result;
        }

        private ResultItem ProcessSub(IDocument document, ResultItem item)
        {
            var docUri = document.BaseUri.CreateUrl();
            var links = document.QuerySelectorAll("a").OfType<IHtmlAnchorElement>()
                .Where(_=>!string.IsNullOrWhiteSpace(_.Href))
                .Select(_ => _.Href).Distinct().Select(_ => _.CreateUrl())
                .Where(_ => _.Host == docUri.Host && _.Href != docUri.Href 
                && !_.Href.ToLowerInvariant().Contains(fileExt[0].ToLowerInvariant())
                && !_.Href.ToLowerInvariant().Contains(fileExt[1].ToLowerInvariant())
                && !_.Href.ToLowerInvariant().Contains(fileExt[2].ToLowerInvariant())
                && !_.Href.ToLowerInvariant().Contains(fileExt[3].ToLowerInvariant())
                && !_.Href.ToLowerInvariant().Contains(fileExt[4].ToLowerInvariant())
                && !_.Href.ToLowerInvariant().Contains(fileExt[5].ToLowerInvariant())
                && !_.Href.ToLowerInvariant().Contains(fileExt[6].ToLowerInvariant())
                && !_.Href.ToLowerInvariant().Contains(fileExt[7].ToLowerInvariant())
                && !_.Href.ToLowerInvariant().Contains(fileExt[8].ToLowerInvariant())).Take(70).ToList();
            foreach(var link in links)
            {
                ProcessUrl(link, item);
                if (item.FilledIn())
                {
                    break;
                }
            }
            return item;
        }

        private ResultItem ProcessUrl(Url url, ResultItem resultItem)
        {
            var document = GetDocument(url);
            if (document != null)
            {
                if (document.StatusCode == System.Net.HttpStatusCode.OK)
                {
	                if (!resultItem.UseCard)
	                {
		                resultItem.UseCard = GetUseCard(document);
					}
	                if (!resultItem.TempWithoutCard)
	                {
		                resultItem.TempWithoutCard = GetTempWithoutCard(document);
	                }
					var email = GetEmail(document);
					if (!string.IsNullOrWhiteSpace(email))
	                {
		                resultItem.Email = resultItem.Email.MergeSeparatedString(email);
	                }
					var phone = GetPhone(document);
					if (!string.IsNullOrWhiteSpace(phone))
					{
						resultItem.Phone = resultItem.Phone.MergeSeparatedString(phone);
	                }
                    if (string.IsNullOrWhiteSpace(resultItem.Inn))
                    {
                        resultItem.Inn = GetInn(document);
                    }
	                if (string.IsNullOrWhiteSpace(resultItem.RealName))
	                {
		                resultItem.RealName = GetRealName(document);
	                }
				}
                document.Dispose();
            }
            return resultItem;
        }

        private async Task<IDocument> Load(Url url)
        {
            return await BrowsingContext.New(_config).OpenAsync(url, _token);
        }
        private IDocument GetDocument(Url url)
        {
            try
            {
                var task = Load(url);
                task.Wait(_token);
                return task.Result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        #region Parse
        private bool GetUseCard(IDocument document)
        {
            return document.All.Any(_ =>
                 _.TextContent.Contains("Мы принимаем картами") || _.TextContent.Contains("Оплата") 
                 || _.TextContent.Contains("Оплата банковскими картами") || _.TextContent.Contains("Оплата услуг онлайн") ||
                _.TextContent.Contains("Visa") || _.TextContent.Contains("payment"));
        }

	    private string GetRealName(IDocument document)
	    {
		    var result = string.Empty;
		    if (document.Title.Contains("Контакты") || document.Title.Contains("Контактная информация") || document.Title.Contains("О компании"))
		    {
			    var elements = document.All.Where(_ => _.TextContent.Contains("OOO") ||
			                                       _.TextContent.Contains("Компания")||
			                                       _.TextContent.Contains("ИП")||
			                                       _.TextContent.Contains("АО")||
			                                       _.TextContent.Contains("ЗАО"));
				foreach (var item in elements)
				{
					if (!string.IsNullOrWhiteSpace(result))
					{
						break;
					}
					var name = item.TextContent.ExtractRealName();
					if (!string.IsNullOrWhiteSpace(name))
					{
						result = name;
					}
				}

			}
			return result;
	    }

		private bool GetTempWithoutCard(IDocument document)
		{

			if (string.IsNullOrWhiteSpace(document?.DocumentElement?.TextContent))
			{
				return false;
			}
			var textContent = document.DocumentElement.TextContent;
			return (textContent.Contains("Оплата картой временно недоступна") ||
			        textContent.Contains("Оплата пластиковыми картами не принимается")||
					textContent.Contains("Оплата картами временно не работает") ||
			        (textContent.Contains("Оплата пластиковыми картами") && textContent.Contains("не принимается")));
		}
		private string GetInn(IDocument document)
        {
            var result = string.Empty;
            var els = document.All.Where(_ => _.TextContent.IsINN()).ToList();
            if (els.Any())
            {
                foreach(var item in els)
                {
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        break;
                    }
                    var inn = item.TextContent.ExtractInn();
                    if (inn.IsValidINN())
                    {
                        result = inn;
                    }
                }
            }
            return result;
        }
        private string GetPhone(IDocument document)
        {
            var result = string.Empty;
            var phones = document.QuerySelectorAll("a").OfType<IHtmlAnchorElement>()
                .Where(_ => !string.IsNullOrWhiteSpace(_.Href) && _.Href.StartsWith("tel"))
                .Select(_ => new Uri(_.Href)).Select(_=>_.LocalPath).Distinct().ToList();

            if (phones.Any())
            {
                result = String.Join(",", phones.Select(_=>_.NormalizePhone()).Where(_=>!string.IsNullOrWhiteSpace(_)).Distinct());
            }
           
            if (string.IsNullOrWhiteSpace(result))
            {
                var els = document.All.Where(_ => _.TextContent.IsPhone()).ToList();
                if (els.Any())
                {
                    var results = new List<string>();
                    foreach(var el in els)
                    {
                        results.Add(el.TextContent.ExtractPhone());
                    }
                    result = String.Join(",", results.Select(_ => _.NormalizePhone()).Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct());
                }
            }
            return result;
        }
	    private string GetEmail(IDocument document)
	    {
		    var result = string.Empty;
		    var emails = document.DocumentElement.OuterHtml.ExtractEmails().ToList();
			var tempEmails = new List<string>();
		    if (!emails.Any()) return result;
		    foreach (var email in emails)
		    {
			    try
			    {
				    var uri = new Uri($"mailto:{email}");
				    if ( document.Domain.Contains(uri.Host))
				    {
					    tempEmails.Add(email);
				    }
			    }
			    catch (UriFormatException)
			    {
				    //check incorrect uri
			    }
		    }
		    result = string.Join(",", tempEmails.Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct());
		    return result;
	    }

		
		#endregion Parse
	}
}
