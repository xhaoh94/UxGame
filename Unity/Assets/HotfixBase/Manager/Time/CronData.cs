using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ux
{
    public struct CronData
    {
        enum CronType
        {
            Second,
            Minute,
            Hour,
            Day,
            Month,
            Week,
            Year,
        }

        struct CronUnitParse
        {
            private CronType type { get; set; }

            public List<int> values { get; private set; }

            //特殊字段 例如 ？L W
            public string special { get; private set; }
            public int specialNum { get; private set; }

            readonly Dictionary<string, int> weekStrToNum;
            readonly Dictionary<string, int> monthStrToNum;
            public CronUnitParse(CronType _type)
            {
                type = _type;
                special = string.Empty;
                specialNum = 0;
                values = new List<int>();
                weekStrToNum = new Dictionary<string, int>()
                {
                    { "SUN", 1 },
                    { "MON", 2 },
                    { "TUE", 3 },
                    { "WED", 4 },
                    { "THU", 5 },
                    { "FRI", 6 },
                    { "SAT", 7 }
                };
                monthStrToNum = new Dictionary<string, int>()
                {
                    { "JAN", 1 },
                    { "FEB", 2 },
                    { "MAR", 3 },
                    { "APR", 4 },
                    { "MAY", 5 },
                    { "JUN", 6 },
                    { "JUL", 7 },
                    { "AUG", 8 },
                    { "SEP", 9 },
                    { "OCT", 10 },
                    { "NOV", 11 },
                    { "DEC", 12 }
                };
                _min = 0;
                _max = 2099;
            }

            public void Parse()
            {
                for (var i = Min; i <= Max; i++)
                {
                    values.Add(i);
                }
            }

            public void Parse(string cronUnit)
            {
                cronUnit = cronUnit.ToUpper().Trim();
                if (Parse0(cronUnit)) return;
                if (Parse1(cronUnit)) return;
                if (Parse2(cronUnit)) return;
                if (Parse3(cronUnit)) return;
                if (Parse5(cronUnit)) return;
                if (Parse4(cronUnit)) return;
                if (Parse6(cronUnit)) return;
                if (Parse7(cronUnit)) return;
                Parse8(cronUnit);
            }

            // 解析 *
            bool Parse0(string cronUnit)
            {
                if (!string.IsNullOrEmpty(cronUnit) && cronUnit != "*") return false;
                for (var i = Min; i <= Max; i++)
                {
                    values.Add(i);
                }

                return true;
            }

            //解析 值
            bool Parse1(string cronUnit)
            {
                int v = -1;
                if (type == CronType.Week && weekStrToNum.TryGetValue(cronUnit, out var temV))
                {
                    v = temV;
                }
                else if (type == CronType.Month && monthStrToNum.TryGetValue(cronUnit, out temV))
                {
                    v = temV;
                }

                if (v == -1 && !int.TryParse(cronUnit, out v))
                {
                    Log.Error($"Cron解析：未知的值[{cronUnit}]");
                    return false;
                }

                values.Add(v);
                return true;
            }

            // 解析 -
            bool Parse2(string cronUnit)
            {
                if (!cronUnit.Contains('-')) return false;
                var crons = cronUnit.Split('-');
                if (crons.Length <= 0) return false;
                var temMin = -1;
                if (type == CronType.Week && weekStrToNum.TryGetValue(crons[0], out var temV))
                {
                    temMin = temV;
                }
                else if (type == CronType.Month && monthStrToNum.TryGetValue(crons[0], out temV))
                {
                    temMin = temV;
                }

                if (temMin == -1 && !int.TryParse(crons[0], out temMin))
                {
                    throw new Exception($"Cron表达式{type}域,'-' 前必须是整数");
                }

                var temMax = -1;
                if (type == CronType.Week && weekStrToNum.TryGetValue(crons[1], out temV))
                {
                    temMax = temV;
                }
                else if (type == CronType.Month && monthStrToNum.TryGetValue(crons[1], out temV))
                {
                    temMax = temV;
                }

                if (temMax == -1 && !int.TryParse(crons[1], out temMax))
                {
                    throw new Exception($"Cron表达式{type}域,'-' 后必须是整数");
                }


                if (temMin > temMax)
                {
                    var tem = temMin;
                    temMin = temMax;
                    temMax = tem;
                }

                if (temMin < Min || temMax > Max)
                {
                    throw new Exception($"Cron表达式{type}域，不在${Min}-${Max}范围内");
                }

                for (var i = temMin; i <= temMax; i++)
                {
                    values.Add(i);
                }

                return true;
            }

            // 解析 ,
            bool Parse3(string cronUnit)
            {
                if (!cronUnit.Contains(',')) return false;
                var crons = cronUnit.Split(',');
                if (crons.Length <= 0) return false;
                foreach (var t in crons)
                {
                    var c = t.Trim();
                    if (string.IsNullOrEmpty(c)) continue;
                    int v = -1;
                    if (type == CronType.Week && weekStrToNum.TryGetValue(c, out var temV))
                    {
                        v = temV;
                    }
                    else if (type == CronType.Month && monthStrToNum.TryGetValue(c, out temV))
                    {
                        v = temV;
                    }

                    if (v == -1 && !int.TryParse(c, out v))
                    {
                        throw new Exception($"Cron表达式{type}域,',' 前后必须是整数");
                    }

                    if (v < Min || v > Max)
                    {
                        throw new Exception($"Cron表达式{type}区间不在${Min}-${Max}范围内");
                    }

                    values.Add(v);
                }

                return true;
            }

            // 解析 /
            bool Parse4(string cronUnit)
            {
                if (!cronUnit.Contains('/')) return false;
                var crons = cronUnit.Split('/');
                if (crons.Length <= 0) return false;
                if (!int.TryParse(crons[0], out var s))
                {
                    s = 0;
                }

                if (s < Min || s > Max)
                {
                    throw new Exception($"Cron表达式{type}域,不在${Min}-${Max}范围内");
                }

                if (!int.TryParse(crons[1], out var add))
                {
                    throw new Exception($"Cron表达式{type}域,'/' 后必须是整数");
                }

                for (var i = s; i <= Max; i += add)
                {
                    values.Add(i);
                }

                return true;
            }

            //解析 ？
            bool Parse5(string cronUnit)
            {
                if (cronUnit != "?") return false;
                if (type != CronType.Day && type != CronType.Week)
                {
                    throw new Exception("Cron解析：?解析,仅日期和星期域支持");
                }

                special = cronUnit;
                return true;
            }

            //解析 L
            bool Parse6(string cronUnit)
            {
                if (!cronUnit.EndsWith("L")) return false;
                if (type != CronType.Day && type != CronType.Week)
                {
                    throw new Exception("L解析,仅日期和星期域支持");
                }

                if (cronUnit != "L")
                {
                    if (type != CronType.Week)
                    {
                        throw new Exception($"Cron表达式{type}域，L格式错误");
                    }

                    var match = Regex.Match(cronUnit, "(?<v>[1-7]|[A-Z]+)(?=L)");
                    if (!match.Success)
                    {
                        throw new Exception($"Cron表达式{type}域，L格式错误");
                    }

                    var temNum = 0;
                    var temValue = match.Groups["v"].Value;
                    if (type == CronType.Week && weekStrToNum.TryGetValue(temValue, out var v))
                    {
                        temNum = v;
                    }

                    if (temNum == 0 && !int.TryParse(temValue, out temNum))
                    {
                    }

                    if (temNum < Min || temNum > Max)
                    {
                        throw new Exception($"Cron表达式{type}域，L格式错误");
                    }

                    specialNum = temNum;
                }
                else
                {
                    specialNum = 0;
                }

                special = "L";
                return true;
            }

            //解析 W
            bool Parse7(string cronUnit)
            {
                if (!cronUnit.EndsWith("W")) return false;
                if (type != CronType.Day)
                {
                    throw new Exception("W解析,仅日期域支持");
                }

                var match = Regex.Match(cronUnit, "(?<v>[1-7,L]+)(?=W)");
                if (!match.Success)
                {
                    throw new Exception("Cron表达式{type}域,W格式错误");
                }

                if (int.TryParse(match.Groups["v"].Value, out var v))
                {
                    if (v < Min || v > Max)
                    {
                        throw new Exception("Cron表达式{type}域,W格式错误");
                    }

                    special = "W";
                    specialNum = v;
                }
                else
                {
                    special = cronUnit;
                }

                return false;
            }

            //解析 #
            bool Parse8(string cronUnit)
            {
                if (!cronUnit.Contains("#")) return false;
                if (type != CronType.Week)
                {
                    throw new Exception("#解析,仅星期域支持");
                }

                var match = Regex.Match(cronUnit, "(?<v1>[1-7]|[A-Z]+)#(?<v2>[1-9]+)");
                var temNum1 = 0;
                var temValue1 = match.Groups["v1"].Value;
                if (type == CronType.Week && weekStrToNum.TryGetValue(temValue1, out temNum1))
                {
                }

                if (temNum1 == 0 && !int.TryParse(temValue1, out temNum1))
                {
                    throw new Exception($"#解析,#前参数不在${Min}-${Max}范围内");
                }

                var temValue2 = match.Groups["v2"].Value;
                if (!int.TryParse(temValue2, out var temNum2) || temNum2 < 1 || temNum2 > 5)
                {
                    throw new Exception($"#解析,#后参数不在必须是整数，且在1-5范围内");
                }

                values.Add(temNum1);
                specialNum = temNum2;
                special = "#";
                return true;
            }

            int _min;
            public int Min
            {
                get
                {
                    switch (type)
                    {
                        case CronType.Week:
                            return 1;
                        case CronType.Month:
                            return 1;
                        case CronType.Day:
                            return 1;
                        case CronType.Hour:
                            return 0;
                        case CronType.Minute:
                            return 0;
                        case CronType.Second:
                            return 0;
                    }

                    return _min;
                }
                set
                {
                    _min = value;
                }
            }


            int _max;
            public int Max
            {
                get
                {
                    switch (type)
                    {
                        case CronType.Week:
                            return 7;
                        case CronType.Month:
                            return 12;
                        case CronType.Day:
                            return 31;
                        case CronType.Hour:
                            return 23;
                        case CronType.Minute:
                            return 59;
                        case CronType.Second:
                            return 59;
                    }

                    return _max;
                }
                set
                {
                    _max = value;
                }
            }

            public int GetAdd(int value, int temMax = 0, bool isReset = true)
            {
                if (temMax == 0) temMax = Max;
                foreach (var v in values)
                {
                    if (v > temMax) break;
                    if (v >= value)
                    {
                        return v - value;
                    }
                }

                if (!isReset) return -1;
                return temMax - value + values[0] + (Min == 0 ? 1 : 0);
            }
        }

        CronData(string cron)
        {
            _isParse = false;
            Second = new CronUnitParse(CronType.Second);
            Minute = new CronUnitParse(CronType.Minute);
            Hour = new CronUnitParse(CronType.Hour);
            Day = new CronUnitParse(CronType.Day);
            Week = new CronUnitParse(CronType.Week);
            Month = new CronUnitParse(CronType.Month);
            Year = new CronUnitParse(CronType.Year);
            yearCron = null;
            Parse(cron);
        }

        CronUnitParse Second;
        CronUnitParse Minute;
        CronUnitParse Hour;
        CronUnitParse Day;
        CronUnitParse Week;
        CronUnitParse Month;
        CronUnitParse Year;

        private bool _isParse;

        private static OverdueMap<string, CronData> _caches;

        public static CronData Create(string cron)
        {
            if (_caches == null)
            {
                _caches = new OverdueMap<string, CronData>(60);
            }

            if (_caches.TryGetValue(cron, out var data))
            {
                return data;
            }

            data = new CronData(cron);
            _caches.Add(cron, data);
            return data;
        }
        public static void Release()
        {
            _caches?.Clear();
        }

        string yearCron;
        void Parse(string cron)
        {
            try
            {
                var crons = cron.Split(' ');
                if (crons[3] == "?" && crons[5] == "?")
                {
                    throw new Exception("日期和周域不可同时不指定");
                }

                if (crons[3] != "?" && crons[5] != "?")
                {
                    throw new Exception("日期和周域不可同时存在指定数据");
                }

                Second.Parse(crons[0]);
                Minute.Parse(crons[1]);
                Hour.Parse(crons[2]);
                Day.Parse(crons[3]);
                Month.Parse(crons[4]);
                Week.Parse(crons[5]);
                if (crons.Length > 6)
                {
                    yearCron = crons[6];
                }
            }
            catch
            {
                Log.Error($"解析Cron表达式[{cron}]失败");
            }
        }

        public DateTime GetLateTime(DateTime now)
        {
            return GetNext(now);
        }

        public DateTime GetNext(DateTime time)
        {
            time = time.AddMilliseconds(1000 - time.Millisecond + 1);

            if (!_isParse && Year.Min != time.Year)
            {
                Year.Min = time.Year;
                Year.Max = time.Year + 99;
                if (string.IsNullOrEmpty(yearCron))
                {
                    Year.Parse();
                }
                else
                {
                    Year.Parse(yearCron);
                }
                _isParse = true;
            }
            
            int add = Second.GetAdd(time.Second);
            time = AddSecond(time, add);
            add = Minute.GetAdd(time.Minute);
            time = AddMinute(time, add);
            add = Hour.GetAdd(time.Hour);
            time = AddHour(time, add);
            time = ParseDay(time);
            if (time == default) return default;
            time = ParseWeek(time);
            if (time == default) return default;
            
            return time;
        }

        public List<DateTime> GetExeTime(DateTime time, int cnt = 1)
        {
            time = time.AddMilliseconds(1000 - time.Millisecond + 1);

            if (!_isParse && Year.Min != time.Year)
            {
                Year.Min = time.Year;
                Year.Max = time.Year + 99;
                if (string.IsNullOrEmpty(yearCron))
                {
                    Year.Parse();
                }
                else
                {
                    Year.Parse(yearCron);
                }
                _isParse = true;
            }

            List<DateTime> nexts = new List<DateTime>();
            for (var i = 0; i < cnt; i++)
            {
                int add = Second.GetAdd(time.Second);
                time = AddSecond(time, add);
                add = Minute.GetAdd(time.Minute);
                time = AddMinute(time, add);
                add = Hour.GetAdd(time.Hour);
                time = AddHour(time, add);
                time = ParseDay(time);
                if (time == default) break;
                time = ParseWeek(time);
                if (time == default) break;
                nexts.Add(time);
                time = AddSecond(time, 1);
            }

            return nexts;
        }

        DateTime ParseDay(DateTime time)
        {
            var special = Day.special;
            var specialNum = Day.specialNum;
            int add;
            if (special == "?") return time;
            time = Check(time);
            if (time == default) return default;
            var days = DateTime.DaysInMonth(time.Year, time.Month);
            switch (special)
            {
                case "LW":
                    {
                        var temDay = time.Day;
                        var c = days - time.Day; //获取最后一天跟当天的差数                            
                        time = AddDay(time, c);
                        while (time.GetWeek() == 1 || time.GetWeek() == 7) //周六、日
                        {
                            time = AddDay(time, -1);
                        }

                        return time.Day < temDay ? //日期已经过去了,跳转到下个月1号
                            ParseDay(AddMonth(time, 1)) : time;
                    }
                //指定日期超出当月天数,跳转到下个月1号
                case "W" when specialNum > days:
                    return ParseDay(AddMonth(time, 1));
                case "W":
                    {
                        var temDay = time.Day;
                        add = specialNum - time.Day;
                        time = AddDay(time, add); //跳到指定日期                            
                        if (time.GetWeek() == 1) //周日
                        {
                            if (time.Day < days)
                            {
                                time = AddDay(time, 1);
                            }
                        }
                        else if (time.GetWeek() == 7) //周六
                        {
                            if (time.Day > 1)
                            {
                                time = AddDay(time, -1);
                            }
                        }

                        if (time.GetWeek() == 1 || time.GetWeek() == 7 || temDay > time.Day) //指定日期小于当前日期，跳下个月
                        {
                            return ParseDay(AddMonth(time, 1));
                        }

                        return time;
                    }
                case "L":
                    add = days - time.Day; //最后一天
                    time = AddDay(time, add);
                    return time;
            }

            add = Day.GetAdd(time.Day, days, false);
            if (add == -1) //后续没有可选择日期，跳下个月
            {
                return ParseDay(AddMonth(time, 1, Day.values[0]));
            }

            time = AddDay(time, add);
            return time;
        }

        DateTime ParseWeek(DateTime time)
        {
            var special = Week.special;
            var specialNum = Week.specialNum;
            int add;
            int days;
            if (special == "?") return time;
            time = Check(time);
            if (time == default) return default;
            if (special == "#")
            {
                var temDay = time.Day;
                var c = 1 - time.Day; //跟月第一天差数
                time = AddDay(time, c);
                int targetWeek = Week.values[0];
                int temWeek = time.GetWeek();
                if (targetWeek >= temWeek)
                {
                    add = targetWeek - temWeek + (7 * (specialNum - 1));
                }
                else
                {
                    add = 7 - (temWeek - targetWeek) + (7 * (specialNum - 1));
                }

                if (time.Day + add < temDay) //指定周几，已经过去了，跳下个月
                {
                    return ParseWeek(AddMonth(time, 1));
                }

                days = DateTime.DaysInMonth(time.Year, time.Month);
                if (time.Day + add > days) //指定周几，超出当月天数，跳下个月
                {
                    return ParseWeek(AddMonth(time, 1));
                }

                time = AddDay(time, add);
                return time;
            }
            else if (special == "L")
            {
                bool isNeedLoop = true;
                if (specialNum == 0)
                {
                    specialNum = 7;
                    isNeedLoop = false;
                }

                days = DateTime.DaysInMonth(time.Year, time.Month);
                int temWeek = time.GetWeek();
                var temDay = time.Day;
                if (temWeek <= specialNum)
                {
                    temDay += specialNum - temWeek;
                }
                else
                {
                    temDay += 7 - (temWeek - specialNum);
                }

                if (temDay > days) //指定周几，超出当月天数，跳下个月
                {
                    return ParseWeek(AddMonth(time, 1));
                }

                if (isNeedLoop)
                {
                    while (temDay + 7 <= days)
                    {
                        temDay += 7;
                    }
                }

                add = temDay - time.Day;
                time = AddDay(time, add);
                return time;
            }

            add = Week.GetAdd(time.GetWeek());
            days = DateTime.DaysInMonth(time.Year, time.Month);
            if (time.Day + add > days) //指定周几，超出当月天数，跳下个月
            {
                return ParseWeek(AddMonth(time, 1));
            }

            time = AddDay(time, add);
            return time;
        }

        DateTime AddSecond(DateTime time, int add)
        {
            if (add == 0) return time;
            return time.AddSeconds(add);
        }

        DateTime AddMinute(DateTime time, int add)
        {
            if (add == 0) return time;
            if (add > 0)
            {
                time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, Second.values[0]);
            }

            return time.AddMinutes(add);
        }

        DateTime AddHour(DateTime time, int add)
        {
            if (add == 0) return time;
            if (add > 0)
            {
                time = new DateTime(time.Year, time.Month, time.Day, time.Hour, Minute.values[0], Second.values[0]);
            }

            return time.AddHours(add);
        }

        DateTime AddDay(DateTime time, int add)
        {
            switch (add)
            {
                case 0:
                    return time;
                case > 0:
                    time = new DateTime(time.Year, time.Month, time.Day, Hour.values[0], Minute.values[0],
                        Second.values[0]);
                    break;
            }

            return time.AddDays(add);
        }

        DateTime AddMonth(DateTime time, int add, int day = 1)
        {
            if (add == 0) return time;
            var newMonth = time.Month + add;
            var newYear = time.Year;
            while (newMonth > 12)
            {
                newMonth -= 12;
                newYear++;
            }

            return new DateTime(newYear, newMonth, day, Hour.values[0], Minute.values[0], Second.values[0]);
        }

        DateTime Check(DateTime time)
        {
            int temMonth = time.Month;
            int temYear = time.Year;
            var add = Month.GetAdd(time.Month);
            if (add != 0)
            {
                time = time.AddMonths(add);
            }

            add = Year.GetAdd(time.Year, 0, false);
            if (add == -1)
            {
                return default;
            }

            if (add != 0)
            {
                time = time.AddYears(add);
            }

            if (temMonth != time.Month || temYear != time.Year) //切换月或年，重置天数为月第一天
            {
                time = new DateTime(time.Year, time.Month, 1, Hour.values[0], Minute.values[0], Second.values[0]);
            }

            return time;
        }
    }
}