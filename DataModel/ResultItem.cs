using System.Net;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Parser.DataModel
{
    public class ResultItem
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public bool UseCard { get; set; }
	    public bool TempWithoutCard { get; set; }
		public string Phone { get; set; }
	    public string Email { get; set; }
        public string Inn { get; set; }
	    public string RealName { get; set; }
		public HttpStatusCode Status { get; internal set; }

        public bool FilledIn()
        {
	        return false;//!string.IsNullOrWhiteSpace(Phone) && !string.IsNullOrWhiteSpace(Inn) && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(RealName);
		}
    }

    public sealed class ResultItemMap : CsvClassMap<ResultItem>
    {
        public ResultItemMap()
        {
            Map(m => m.Url).Name("Ссылка").NameIndex(0);
            Map(m => m.Title).Name("Наименование").NameIndex(1);
            Map(m => m.UseCard).Name("Использование карты").TypeConverter(new StringBooleanTypeConverter());
			Map(m => m.TempWithoutCard).Name("Оплата картой временно недоступна").TypeConverter(new StringBooleanTypeConverter());
	        Map(m => m.Email).Name("Email").TypeConverter(new StringConverter());
			Map(m => m.Phone).Name("Телефон").NameIndex(2).TypeConverter(new StringConverter());
            Map(m => m.Inn).Name("ИНН").NameIndex(3).TypeConverter(new StringConverter());
	        Map(m => m.RealName).Name("Юр.Лицо").TypeConverter(new StringConverter());

		}
		public static void initConfiguration(CsvConfiguration csvConfiguration)
        {
            csvConfiguration.RegisterClassMap<ResultItemMap>();
            csvConfiguration.TrimFields = true;
        }
    }

    public sealed class ResultItemHarvestMap : CsvClassMap<ResultItem>
    {
        public ResultItemHarvestMap()
        {
            Map(m => m.Url).Name("Ссылка").NameIndex(0);
            Map(m => m.Title).Ignore(true);
            Map(m => m.UseCard).Ignore(true);
	        Map(m => m.TempWithoutCard).Ignore(true);
			Map(m => m.Email).Ignore(true); 
			Map(m => m.Phone).Ignore(true);
            Map(m => m.Inn).Ignore(true);
			Map(m => m.RealName).Ignore(true); 
		}
        public static void initConfiguration(CsvConfiguration csvConfiguration)
        {
            csvConfiguration.RegisterClassMap<ResultItemHarvestMap>();
            csvConfiguration.TrimFields = true;
        }
    }

    public class StringBooleanTypeConverter : DefaultTypeConverter
    {
        public override string ConvertToString(TypeConverterOptions options, object value)
        {
			return value != null && (bool)value ? "Да" : "Нет";
        }
    }


}
