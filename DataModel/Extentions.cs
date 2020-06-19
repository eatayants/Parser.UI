using AngleSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parser.UI
{
    public static class Extentions
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36";

        private static readonly Regex PhoneRegex = new Regex(@"((8|\+7)-?)\W((\d{ 3,5})|\(?\d{3,5}\))\W\d{3}\W((\d{ 2}\W\d{2})|(\d{4}))|(((8|\+7)-?)?\(?\d{3,5}\)?-?\d{1}-?\d{1}-?\d{1}-?\d{1}-?\d{1}((-?\d{1})?-?\d{1})?)", RegexOptions.Compiled);
        
        private static readonly Regex InnRegex = new Regex(@"([0-9]{4})([0-9]{5})([0-9]{1})", RegexOptions.Compiled);

		private static readonly Regex NameRegex = new Regex(@"(^(ООО |ИП |ЗАО |АО |Компания ):([^\s]+))", RegexOptions.Compiled);
		//^([0-9]{4})([0-9]{5})([0-9]{1})$
		private static readonly Regex EmailRegex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", RegexOptions.Compiled);


	    public static string[] ExtractEmails(this string value)
	    {
			var result = new List<string>();
		    var matches = EmailRegex.Matches(value);
		    if (matches.Count > 0)
		    {
			    for (var i = 0; i < matches.Count; i++)
			    {
				    result.Add(matches[i].Value);
			    }
		    }
		    return result.ToArray();
	    }

		private static readonly Regex BooleanRegex = new Regex(@"^(YES|Yes|yes|TRUE|True|true)$", RegexOptions.Compiled);


	    public static string ExtractRealName(this string value)
	    {
			var result = string.Empty;
			if (!string.IsNullOrWhiteSpace(value))
		    {
			    var list = new List<string> {"ООО ", "OOO ", "Компания ", "ИП ", "АО ", "ЗАО " };
			    foreach (var fs in list)
			    {
				    if (!string.IsNullOrWhiteSpace(result))
				    {
					    break;
				    }
				    result = value.ExtractRealNameByFS(fs);
				}
		    }
		    return result;
		}

	    public static string ExtractRealNameByFS(this string value,string fs)
	    {

		    var firstIndex = value.IndexOf(fs, StringComparison.OrdinalIgnoreCase);
		    if (firstIndex < 0)
		    {
			    return string.Empty;
		    }
		    using (var reader = new StringReader(value.Substring(firstIndex)))
		    {
			    var result = reader.ReadLine();
			    if (string.IsNullOrEmpty(result))
			    {
				    return result;
			    }
				var fi = result.IndexOf(",", StringComparison.Ordinal);
			    return fi < 0 ? result : result.Substring(0, fi);
		    }
	    }

		private static int checkSumINN(int n, string inn)
        {
            var s = 0;
            int[] checksum = new[] { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
            for (var i = 1; i < n; i++)
            {
                s += (Convert.ToInt32(inn.Substring(i - 1, 1)) * checksum[11 - n + i]);
            }
            return (s % 11) % 10;
        }
        public static bool IsValidINN(this string inn)
        {
            var len = inn.Length;
            if (len == 10)
                return (Convert.ToInt32(inn.SafeSubstring(9, 1)) == checkSumINN(10, inn));

            if (len == 11)
                return (Convert.ToInt32(inn.SafeSubstring(10, 1)) == checkSumINN(11, inn));

            if (len == 12)
                return (Convert.ToInt32(inn.SafeSubstring(11, 1)) == checkSumINN(12, inn));
            return false;
        }


        public static bool IsINN(this string value)
        {
            return !string.IsNullOrEmpty(value) && InnRegex.IsMatch(value);
        }

        public static string ExtractInn(this string value)
        {
            var result = string.Empty;
            if (!string.IsNullOrWhiteSpace(value))
            {
                var match = InnRegex.Match(value);
                if (match.Success)
                {
                    result = match.Value;
                }
            }
            return result;
        }

        public static bool IsCatalog(this string value)
        {
            return value.IsMailRu() || value.IsYa();
        }

        public static bool IsMailRu(this string value)
        {
            return value.Contains("://list.mail.ru/");
        }

        public static bool IsYa(this string value)
        {
            return value.Contains("://yandex.ru/yaca/");
        }


        public static bool IsEnd(this AngleSharp.Dom.IDocument document)
        {
            var items = document.QuerySelectorAll("*").ToList().Where(_ => _.TextContent.Contains("Страницы с указанным вами адресом в каталоге не существует."));
            return items.Any();
        }
        public static string ToNormalRef(this string value)
        {
            var _value = value;
            if (!string.IsNullOrWhiteSpace(_value))
            {
                if (_value.StartsWith("http")|| _value.StartsWith("https"))
                {
                    return _value;
                }
                _value = $"http://{_value}";
            }
            return _value;
        }

        public static string GetCatalogName(this string value)
        {
            var result = string.Empty;
            if (!string.IsNullOrWhiteSpace(value))
            {
	            if (value.IsFile())
	            {
		            result = "File";
	            }
                if (value.IsCatalog())
                {
                    result = "Catalog";
                }
                if (value.IsMailRu())
                {
                    result = "MailRu";
                }
                else if(value.IsYa())
                {
                    result = "Yandex";
                }
            }
            return result;
        }
        

        public static string RemovePage(this string value)
        {
            var builder = new UriBuilder(value);
            var path = builder.Path;
            var page = path.Substring(path.LastIndexOf("/") + 1);
            if(!string.IsNullOrWhiteSpace(page) && page.Contains(".html"))
            {
                builder.Path = path.Replace(page, string.Empty);
                return builder.Uri.ToString();
            }
            return value;
        }

        public static string GetPageTemplate(this string value)
        {
            if (value.IsYa())
            {
                return "{0}.html";
            }
            else if (value.IsMailRu())
            {
                return "0_1_0_{0}.html";
            }
            else
            {
                throw new Exception("Catalog is not supported");
            }
        }

        public static int GetStartPage(this string value)
        {
            var result = 1;
            try
            {
                var builder = new UriBuilder(value);
                var path = builder.Path;
                var page = path.Substring(path.LastIndexOf("/") + 1);
                if (page.Contains(".html"))
                {
                    var arrPage = page.Split('_');
                    var pageNum = arrPage[(arrPage.Length - 1)].Replace(".html", string.Empty);
                    result = int.Parse(pageNum);
                }
            }
            catch
            {
                //todo logging
            }
            return result;
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> sequence, Func<T, Task> action)
        {
            return Task.WhenAll(sequence.Select(action));
        }

        public static bool IsFile(this string value)
        {
            FileInfo fi = null;
            try
            {
                fi = new FileInfo(value);
            }
            catch (ArgumentException) { }
            catch (System.IO.PathTooLongException) { }
            catch (NotSupportedException) { }
            return !(ReferenceEquals(fi, null));
        }

        public static string ToUniqueNumber(this Guid value)
        {
            return value.ToString("N");
        }

	    public static string MergeSeparatedString(this string first, string second)
	    {
		    var firstArr = string.IsNullOrWhiteSpace(first) ? new List<string>():first.Split(',').ToList();
		    var secondArr = string.IsNullOrWhiteSpace(second) ? new List<string>() : second.Split(',').ToList();
		    firstArr.AddRange(secondArr);
		    return string.Join(",",firstArr.Distinct());
		}

		public static string NormalizePhone(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            var digits = new List<char>();
            foreach (var val in value)
            {
	            if (char.IsDigit(val))
	            {
		            digits.Add(val);
	            }
            }
            var str = new string(digits.ToArray());
            if (str.Length == 7)
            {
                if (string.IsNullOrWhiteSpace(str.Replace("0", "")))
                {
                    return string.Empty;
                }
                return $"+7 495 {str.Substring(0, 3)}-{str.Substring(3, 2)}-{str.Substring(5, 2)}";
            }
            else if (str.Length == 10)
            {
                if (string.IsNullOrWhiteSpace(str.Substring(3, 7).Replace("0", "")))
                {
                    return string.Empty;
                }
                return $"+7 {str.Substring(0, 3)} {str.Substring(3, 3)}-{str.Substring(5, 2)}-{str.Substring(7, 2)}";
            }
            else if (str.Length == 11 && (str.Substring(0, 1)=="8" || str.Substring(0, 1) == "7"))
            {
                if (string.IsNullOrWhiteSpace(str.Substring(3, 7).Replace("0", "")))
                {
                    return string.Empty;
                }
                return
	                $"+{str.Substring(0, 1)} {str.Substring(1, 3)} {str.Substring(4, 3)}-{str.Substring(7, 2)}-{str.Substring(9, 2)}";
            }
            else if (str.Length == 12)
            {
                return string.Empty;
            }
            else 
            {
                return string.Empty; ;
            }
        }

        public static bool IsPhone(this string value)
        {
            return !string.IsNullOrEmpty(value) && PhoneRegex.IsMatch(value.Replace(" ", ""));

        }
        public static string ExtractPhone(this string value)
        {
            var result = string.Empty;
            if (!string.IsNullOrWhiteSpace(value))
            {
                var match = PhoneRegex.Match(value.Replace(" ", ""));
                if (match.Success)
                {
                    result = match.Value.NormalizePhone();
                }
            }
            return result;
        }

        public static string SafeSubstring(this string value, int startIndex, int length)
        {
            return new string((value ?? string.Empty).Skip(startIndex).Take(length).ToArray());
        }
        public static string ToSafeString(this string value)
        {
            return value ?? string.Empty;
        }

        public static string VaildFileName(this string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(value.Where(m => !invalidChars.Contains(m)).ToArray());
        }
        public static void Clear<T>(this BlockingCollection<T> blockingCollection)
        {
            if (blockingCollection == null)
            {
                throw new ArgumentNullException("blockingCollection");
            }

            while (blockingCollection.Count > 0)
            {
                T item;
                blockingCollection.TryTake(out item);
            }
        }

        public static Url CreateUrl(this string address)
        {
            if (File.Exists(address))
            {
                address = "file://localhost/" + address.Replace('\\', '/');
            }

            var lurl = address.ToLower();

            if (!lurl.StartsWith("file://") && !lurl.StartsWith("http://") && !lurl.StartsWith("https://") && !lurl.StartsWith("data:"))
            {
                address = "http://" + address;
            }

            var url = Url.Create(address);
            if (!url.IsInvalid && url.IsAbsolute)
            {
                return url;
            }
            return new Url("localhost");
        }
    }
}
