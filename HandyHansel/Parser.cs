using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;

namespace HandyHansel
{
    public class Parser
    {
        private readonly Dictionary<ParserType, Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<DateTime> >> _allParserFuncs = new Dictionary<ParserType, Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<DateTime>>>
        {
            {ParserType.Date, DateParser},
            {ParserType.Time, TimeParser},
            {ParserType.DateRange, DateRangeParser},
            {ParserType.TimeRange, TimeRangeParser},
            {ParserType.DateTimeRange, DateTimeRangeParser},
            {ParserType.DateTime, DateTimeParser},
        };

        private readonly Dictionary<ParserType, Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<DateTime>>> _funcsToUse;

        public Parser(params ParserType[] parserTypes)
        {
            _funcsToUse = new Dictionary<ParserType, Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<DateTime>>>();
            
            foreach (ParserType type in parserTypes)
            {
                _funcsToUse.Add(type, _allParserFuncs[type]);
            }
        }

        public enum ParserType
        {
            DateTime,
            DateTimeRange,
            TimeRange,
            DateRange,
            Time,
            Date,
        }

        private static ParserType ToParserType(string content)
        {
            return content switch
            {
                "datetimeV2.date" => ParserType.Date,
                "datetimeV2.time" => ParserType.Time,
                "datetimeV2.daterange" => ParserType.DateRange,
                "datetimeV2.timerange" => ParserType.TimeRange,
                "datetimeV2.datetimerange" => ParserType.DateTimeRange,
                "datetimeV2.datetime" => ParserType.DateTime,
                _ => throw new ArgumentException(),
            };
        }

        private static IEnumerable<DateTime> DateTimeParser(IEnumerable<KeyValuePair<string, object>> resolution)
        {
            List<DateTime> temp = new List<DateTime>();
            foreach (KeyValuePair<string, object> pair in resolution)
            {
                List<Dictionary<string, string>> nextResult = (List<Dictionary<string, string>>) pair.Value;
                temp.AddRange(
                    nextResult
                        .Select(
                            dict
                                => new TimexProperty(dict["timex"]))
                        .Select(
                            parsed
                                => new DateTime(parsed.Year ?? DateTime.Now.Year+1, parsed.Month ?? DateTime.Now.Month, parsed.DayOfMonth ?? DateTime.Now.Day, parsed.Hour ?? DateTime.Now.Hour, parsed.Minute ?? DateTime.Now.Minute, 1)));
            }

            return temp;
        }

        private static IEnumerable<DateTime> DateTimeRangeParser(IEnumerable<KeyValuePair<string, object>> resolution)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<DateTime> TimeRangeParser(IEnumerable<KeyValuePair<string, object>> resolution)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<DateTime> DateRangeParser(IEnumerable<KeyValuePair<string, object>> resolution)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<DateTime> TimeParser(IEnumerable<KeyValuePair<string, object>> resolution)
        {
            List<DateTime> temp = new List<DateTime>();
            foreach (KeyValuePair<string, object> pair in resolution)
            {
                List<Dictionary<string, string>> nextResult = (List<Dictionary<string, string>>) pair.Value;
                temp.AddRange(
                    nextResult
                        .Select(
                            dict
                                => new TimexProperty(dict["timex"]))
                        .Select(
                            parsed
                                => new DateTime(DateTime.Now.Year+1, DateTime.Now.Month, DateTime.Now.Day, parsed.Hour ?? DateTime.Now.Hour, parsed.Minute ?? DateTime.Now.Minute, 1)));
            }

            return temp;
        }

        private static IEnumerable<DateTime> DateParser(IEnumerable<KeyValuePair<string, object>> resolution)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<string, DateTime> > DateTimeV2Parse(string content)
        {
            List <ModelResult> modelResults = DateTimeRecognizer.RecognizeDateTime(content, Culture.English);
            List<Tuple<string, DateTime>> result = new List<Tuple<string, DateTime>>();
            foreach (ModelResult modelResult in modelResults)
            {
                try
                {
                    ParserType parserType = ToParserType(modelResult.TypeName);
                    // ReSharper disable once InvertIf
                    if (_funcsToUse.ContainsKey(parserType))
                    {
                        Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<DateTime>> parserFunc = _funcsToUse[parserType];
                        result.AddRange(parserFunc(modelResult.Resolution).Select(time => new Tuple<string, DateTime>(modelResult.Text, time)));   
                    }
                }
                catch (ArgumentException exception)
                {
                    Program.Logger.Log(LogLevel.Debug, exception, "Devs need to implement these parsers. Things are hitting them.");
                }
                
            }
            return result;
        }
        public DateTime DateTimeV2TimeParse(string content)
        {
            return TimeParser(DateTimeRecognizer.RecognizeDateTime(content, Culture.English).First().Resolution).First();
        }
        public static DateTime DateTimeV2DateTimeParse(string content)
        {
            return DateTimeParser(DateTimeRecognizer.RecognizeDateTime(content, Culture.English).First().Resolution).First(e => e.CompareTo(DateTime.Now) > 0);
        }
    }
}