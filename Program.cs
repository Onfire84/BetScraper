using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using System.Globalization;

using System.IO;
using System.Text.RegularExpressions;


namespace Crawler
{
    enum PredResult
    {
        WIN,
        LOST,
        NOT_PREDICTED
    }
    class Prediction
    {
        public string Date;
        public string Encounter;
        public string Pred1;
        public string PredX;
        public string Pred2;

        public int nPred1 = 0;
        public int nPred1X = 0;
        public int nPredX = 0;
        public int nPred2X = 0;
        public int nPred2 = 0;
        public int nPred12 = 0;

        public string Tipp;

        public int Stack = 1;

        //Odds
        public float Odd1 = 0f;
        public float Odd1X = 0f;
        public float OddX = 0f;
        public float Odd2X = 0f;
        public float Odd2 = 0f;
        public float Odd12 = 0f;

        public float WinOdd = 0f;
        public float fConfidence = 0f;

        public string Result = "";

        public PredResult ResultPred = PredResult.NOT_PREDICTED;
    }

    class Program
    {
        static void Main(string[] args)
        {
            BackTestingZuluBet();
            //BackTestingProSoccerGr();
        }



        public static void ScrapPageProSoccer(string url, ref List<Prediction> PredictionList, string Date)
        {
            var webGet = new HtmlWeb();
            var doc = webGet.Load(url);

            var Root = doc.DocumentNode;

            Prediction PredObj = null;

            try
            {
                foreach (HtmlNode Item in doc.DocumentNode.SelectNodes("//tbody/tr[@class]"))
                {
                    PredObj = new Prediction();

                    PredObj.Date = Date;
                    PredObj.Encounter = Item.SelectSingleNode("./td[@onmouseover]").InnerText.Trim();

                    PredObj.Pred1 = Item.SelectSingleNode("./td[4]").InnerText.Trim();
                    PredObj.PredX = Item.SelectSingleNode("./td[5]").InnerText.Trim();
                    PredObj.Pred2 = Item.SelectSingleNode("./td[6]").InnerText.Trim();

                    try
                    {
                        PredObj.nPred1 = Convert.ToInt32(PredObj.Pred1);
                        PredObj.nPredX = Convert.ToInt32(PredObj.PredX);
                        PredObj.nPred2 = Convert.ToInt32(PredObj.Pred2);

                        PredObj.nPred1X = PredObj.nPred1 + PredObj.nPredX;
                        PredObj.nPred12 = PredObj.nPred1 + PredObj.nPred2;
                        PredObj.nPred2X = PredObj.nPred2 + PredObj.nPredX;
                    }
                    catch(Exception e)
                    {
                        //Console.WriteLine(e.Message);
                    }

                    var Tipp = Item.SelectSingleNode("./td[7]/span").Attributes["class"].Value.Trim();

                    if (Tipp.Contains("emp") || Tipp.Equals("-"))
                    {
                        continue;
                    }

                    PredObj.Stack = 1;
                    PredObj.Tipp = Tipp;

                    if (float.TryParse(Item.SelectSingleNode("./td[10]").InnerText, out PredObj.Odd2))
                    {
                        PredObj.Odd1 = float.Parse(Item.SelectSingleNode("./td[8]").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                        PredObj.OddX = float.Parse(Item.SelectSingleNode("./td[9]").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                        PredObj.Odd2 = float.Parse(Item.SelectSingleNode("./td[10]").InnerText, CultureInfo.InvariantCulture.NumberFormat);

                        PredObj.Odd1X = 1 / ((1 / PredObj.Odd1) + (1 / PredObj.OddX));
                        PredObj.Odd12 = 1 / ((1 / PredObj.Odd1) + (1 / PredObj.Odd2));
                        PredObj.Odd2X = 1 / ((1 / PredObj.Odd2) + (1 / PredObj.OddX));
                    }
                    else
                    {
                        continue;
                    }


                    if (Item.SelectSingleNode("./td[15]") != null)
                    {
                        PredObj.Result = Item.SelectSingleNode("./td[15]").InnerText.Trim();
                    }

                    if (String.IsNullOrEmpty(Tipp))
                    {
                        PredObj.ResultPred = PredResult.NOT_PREDICTED;
                    }
                    else
                    {
                        if (Tipp.Contains("tip_r"))
                        {
                            PredObj.ResultPred = PredResult.WIN;

                            if (Tipp.Contains("_r21") || Tipp.Contains("_r12"))
                            {
                                PredObj.Tipp = "21";
                                PredObj.WinOdd = PredObj.Odd12;
                                PredObj.fConfidence = float.Parse(PredObj.Pred1) + float.Parse(PredObj.Pred2);
                            }
                            else if (Tipp.Contains("_rX1") || Tipp.Contains("_r1X"))
                            {
                                PredObj.Tipp = "1X";
                                PredObj.WinOdd = PredObj.Odd1X;
                                PredObj.fConfidence = float.Parse(PredObj.Pred1) + float.Parse(PredObj.PredX);
                            }
                            else if (Tipp.Contains("_rX2") || Tipp.Contains("_r2X"))
                            {
                                PredObj.Tipp = "X2";
                                PredObj.WinOdd = PredObj.Odd2X;
                                PredObj.fConfidence = float.Parse(PredObj.Pred2) + float.Parse(PredObj.PredX);
                            }
                            else if (Tipp.Contains("_r1"))
                            {
                                PredObj.Tipp = "1";
                                PredObj.WinOdd = PredObj.Odd1;
                                PredObj.fConfidence = float.Parse(PredObj.Pred1);
                            }
                            else if (Tipp.Contains("_r2"))
                            {
                                PredObj.Tipp = "2";
                                PredObj.WinOdd = PredObj.Odd2;
                                PredObj.fConfidence = float.Parse(PredObj.Pred2);
                            }
                        }
                        else
                        {
                            PredObj.ResultPred = PredResult.LOST;

                            if (Tipp.Contains("_w21") || Tipp.Contains("_w12"))
                            {
                                PredObj.Tipp = "21";
                                PredObj.WinOdd = PredObj.Odd12;
                                PredObj.fConfidence = float.Parse(PredObj.Pred1) + float.Parse(PredObj.Pred2);
                            }
                            else if (Tipp.Contains("_w1X") || Tipp.Contains("_wX1"))
                            {
                                PredObj.Tipp = "1X";
                                PredObj.WinOdd = PredObj.Odd1X;
                                PredObj.fConfidence = float.Parse(PredObj.Pred1) + float.Parse(PredObj.PredX);
                            }
                            else if (Tipp.Contains("_w2X") || Tipp.Contains("_wX2"))
                            {
                                PredObj.Tipp = "X2";
                                PredObj.WinOdd = PredObj.Odd2X;
                                PredObj.fConfidence = float.Parse(PredObj.Pred2) + float.Parse(PredObj.PredX);
                            }
                            else if (Tipp.Contains("_w1"))
                            {
                                PredObj.Tipp = "1";
                                PredObj.WinOdd = PredObj.Odd1;
                                PredObj.fConfidence = float.Parse(PredObj.Pred1);
                            }
                            else if (Tipp.Contains("_w2"))
                            {
                                PredObj.Tipp = "2";
                                PredObj.WinOdd = PredObj.Odd2;
                                PredObj.fConfidence = float.Parse(PredObj.Pred2);
                            }
                        }
                    }

                    PredictionList.Add(PredObj);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void BackTestingProSoccerGr()
        {
            //DateTime dtStart = DateTime.Now.AddDays(-1);
            DateTime dtEnd = new DateTime(2016, 06 /* <- Month*/, 1 /* <- Day*/);
            DateTime dtStart = new DateTime(2016, 08 /* <- Month*/, 21 /* <- Day*/);

            List<Prediction> PredictionList = new List<Prediction>();

            foreach (DateTime day in EachDay(dtStart, dtEnd))
            {
                string sDate = String.Format("{0:yyyy-MM-dd}", day);
                /*http://www.prosoccer.gr/en/2016/07/soccer-predictions-2016-07-23.html*/
                string url = $"http://www.prosoccer.gr/en/{String.Format("{0:yyyy}", day)}/{String.Format("{0:MM}", day)}/soccer-predictions-{sDate}.html";

                Console.WriteLine("Analysiere " + sDate);

                ScrapPageProSoccer(url, ref PredictionList, sDate);               
            }

            Console.Clear();

            var Predictions = from Item in PredictionList
                              where
                              //Item.Tipp == Tipp &&
                              //(Item.Tipp == "2") && /*(Item.Tipp != "2") &&*/
                              //Item.fConfidence >= 90 &&
                              Item.WinOdd >= 1.3f && Item.WinOdd <= 1.45f &&
                              //Item.Tipp == "2" &&
                              //Item.Stack >= Stack &&
                              Item.ResultPred != PredResult.NOT_PREDICTED
                              orderby Item.WinOdd
                              select Item;

            GetCashFlowProsoccerGr(Predictions);


            Console.Write("Press any key to continue");
            Console.ReadLine();
        }

        

        public static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date >= thru.Date; day = day.AddDays(-1))
            {
                yield return day;
            }
        }

        public static void ScrapPage(string url, ref List<Prediction> PredictionList)
        {
            var webGet = new HtmlWeb();
            var doc = webGet.Load(url);

            var Root = doc.DocumentNode;

            try
            {
                foreach (HtmlNode Item in doc.DocumentNode.SelectNodes("//tr[@bgcolor and not(contains(@class, 'prediction_min'))]")) //"//tr[@bgcolor]"
                {
                    Prediction PredObj = new Prediction();

                    PredObj.Date = Item.SelectSingleNode("./td[1]").InnerText.Trim();
                    PredObj.Encounter = Item.SelectSingleNode("./td[2]").InnerText.Trim();

                    HtmlNodeCollection NodeCollection = Item.SelectNodes("./td[@class='prob2 prediction_full']");

                    PredObj.Pred1 = Item.SelectSingleNode("./td[@class='prob2 prediction_full'][1]").InnerText;
                    PredObj.PredX = Item.SelectSingleNode("./td[@class='prob2 prediction_full'][2]").InnerText;
                    PredObj.Pred2 = Item.SelectSingleNode("./td[@class='prob2 prediction_full'][3]").InnerText;

                    PredObj.nPred1 = Convert.ToInt32(PredObj.Pred1.Remove(PredObj.Pred1.Length - 1));
                    PredObj.nPredX = Convert.ToInt32(PredObj.PredX.Remove(PredObj.PredX.Length - 1));
                    PredObj.nPred2 = Convert.ToInt32(PredObj.Pred2.Remove(PredObj.Pred2.Length - 1));

                    PredObj.nPred1X = PredObj.nPred1 + PredObj.nPredX;
                    PredObj.nPred12 = PredObj.nPred1 + PredObj.nPred2;
                    PredObj.nPred2X = PredObj.nPred2 + PredObj.nPredX;

                    var Tipp = Item.SelectSingleNode("./td[@align]/font/b").InnerText.Trim();

                    if(String.IsNullOrEmpty(Tipp.ToString()))
                    {
                        continue;
                    }

                    var Stack = Item.SelectSingleNode("./td[@align='center'][5]").InnerText.Trim();

                    if (String.IsNullOrEmpty(Stack))
                    {
                        Stack = "1";
                    }

                    PredObj.Stack = Convert.ToInt32(Stack);
                    PredObj.Tipp = Tipp;

                    PredObj.Odd1 = float.Parse(Item.SelectSingleNode("./td[@class='aver_odds_full'][1]").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                    PredObj.OddX = float.Parse(Item.SelectSingleNode("./td[@class='aver_odds_full'][2]").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                    PredObj.Odd2 = float.Parse(Item.SelectSingleNode("./td[@class='aver_odds_full'][3]").InnerText, CultureInfo.InvariantCulture.NumberFormat);

                    PredObj.Odd1X = 1 / ((1 / PredObj.Odd1) + (1 / PredObj.OddX));
                    PredObj.Odd12 = 1 / ((1 / PredObj.Odd1) + (1 / PredObj.Odd2));
                    PredObj.Odd2X = 1 / ((1 / PredObj.Odd2) + (1 / PredObj.OddX));

                    PredObj.Result = Item.SelectSingleNode("./td[@align='center'][10]").InnerText.Trim();

                    if (!String.IsNullOrEmpty(PredObj.Result))
                    {
                        if (Item.SelectSingleNode("./td[@bgcolor='Yellow']") != null)
                        {
                            PredObj.ResultPred = PredResult.NOT_PREDICTED;
                        }
                        else if (Item.SelectSingleNode("./td[@bgcolor='Lime']") != null)
                        {
                            PredObj.ResultPred = PredResult.WIN;
                        }
                        else
                        {
                            PredObj.ResultPred = PredResult.LOST;
                        }
                    }
                    else
                    {
                        PredObj.ResultPred = PredResult.NOT_PREDICTED;
                    }

                    switch (PredObj.Tipp)
                    {
                        case "1":
                            PredObj.WinOdd = PredObj.Odd1;
                            PredObj.fConfidence = (float)PredObj.nPred1;
                            break;
                        case "2":
                            PredObj.WinOdd = PredObj.Odd2;
                            PredObj.fConfidence = (float)PredObj.nPred2;
                            break;
                        case "1X":
                            PredObj.WinOdd = PredObj.Odd1X;
                            PredObj.fConfidence = (float)PredObj.nPred1X;
                            break;
                        case "X2":
                            PredObj.WinOdd = PredObj.Odd2X;
                            PredObj.fConfidence = (float)PredObj.nPred1X;
                            break;
                        case "12":
                            PredObj.WinOdd = PredObj.Odd12;
                            PredObj.fConfidence = (float)PredObj.nPred12;
                            break;
                        case "X":
                            PredObj.WinOdd = PredObj.OddX;
                            PredObj.fConfidence = (float)PredObj.nPredX;
                            break;
                    }

                    PredictionList.Add(PredObj);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void BackTestingZuluBet()
        {
            DateTime dtStart = DateTime.Now.AddDays(-1);
            DateTime dtEnd = new DateTime(2016, 8 /* <- Month*/, 24 /* <- Day*/);

            List<Prediction> PredictionList = new List<Prediction>();

            foreach (DateTime day in EachDay(dtStart, dtEnd))
            {
                //20-08-2016
                string sDate = String.Format("{0:dd-MM-yyyy}", day);
                string urlZulu = $"http://de.zulubet.com/tipps-{sDate}.html";
                Console.WriteLine("Analysiere " + sDate);
                ScrapPage(urlZulu, ref PredictionList/*DayList*/);
            }

            GetWithFilter(ref PredictionList,
                    "1X", /*Prediction Type*/
                    75/*Prediction %     >=*/,
                    1,
                    1.4f/*Odd*/
                    );

            Console.Write("Press any key to continue");
            var name = Console.ReadLine();
        }

        static void GetCashFlowProsoccerGr(IEnumerable<Prediction> PredictionCollection/*, float OddThreshold, int Percentage = 1*/)
        {
            float fAvgOdds = 0;
            float fCashFlow = 0;

            int nCorrect = 0;
            int nFalse = 0;
            int nCount = 0;
            int nStackAvg = 0;

            string sDate = "";

            foreach (var Encounter in PredictionCollection)
            {
                int nPercentage = 0;
                float fCash = 0;
                float fOdd = Encounter.WinOdd;//GetOdd(Encounter, out nPercentage);

                //if (fOdd < OddThreshold || nPercentage < Percentage)
                //{
                //    continue;
                //}

                int nStack = Convert.ToInt32(Encounter.Stack);

                nStack = 1;

                nStackAvg += nStack;
                fAvgOdds += fOdd;

                float fOddInPercent = (1 / fOdd) * 100;
                float fConfidence = (float)nPercentage - fOddInPercent;

                bool bValue = fConfidence < 0 ? false : true;

                if (Encounter.ResultPred == PredResult.LOST)
                {
                    nFalse++;
                    fCash = -nStack;
                }
                else if (Encounter.ResultPred == PredResult.WIN)
                {
                    nCorrect++;
                    fCash = (nStack * fOdd) - nStack;
                }

                fCashFlow += fCash;

                if(!sDate.Equals(Encounter.Date))
                {
                    //Console.WriteLine(String.Empty);
                }

                Console.WriteLine("{0,10}{1,60}{2,15}{3,15}{4,15}{5,15}{6,15}{7,15}",
                Encounter.Date,
                Encounter.Encounter,
                Encounter.ResultPred.ToString(),
                string.Format("{0:0.000}", fCash),
                string.Format("{0:0.000}", Encounter.WinOdd),
                string.Format("{0:0.000}", fCashFlow),
                Encounter.fConfidence,
                Encounter.Tipp
                );

                

                sDate = Encounter.Date;

                ++nCount;
            }

            Console.WriteLine($"Sum encounter: {nCount} - correct: {nCorrect} - false: {nFalse}");
            Console.WriteLine($"Stack average: {(float)nStackAvg / (float)nCount}");
            Console.WriteLine($"Odds average: {(float)fAvgOdds / (float)nCount}");
        }

        static void GetCashFlowZuluBet(IEnumerable<Prediction> PredictionCollection, float? OddThreshold, int? Percentage)
        {
            float fAvgOdds = 0;
            float fCashFlow = 0;

            int nCorrect = 0;
            int nFalse = 0;
            int nCount = 0;
            int nStackAvg = 0;

            foreach (var Encounter in PredictionCollection)
            {
                int nPercentage = 0;
                float fCash = 0;
                float fOdd = Encounter.WinOdd;//GetOdd(Encounter, out nPercentage);

                //if (fOdd < OddThreshold || nPercentage < Percentage)
                //{
                //    continue;
                //}

                int nStack = Convert.ToInt32(Encounter.Stack);

                nStack = 1;

                nStackAvg += nStack;
                fAvgOdds += fOdd;

                float fOddInPercent = (1 / fOdd) * 100;
               

                bool bValue = Encounter.fConfidence > fOddInPercent ? true : false;

                if (Encounter.ResultPred == PredResult.LOST)
                {
                    nFalse++;
                    fCash = -nStack;
                }
                else if (Encounter.ResultPred == PredResult.WIN)
                {
                    nCorrect++;
                    fCash = (nStack * fOdd) - nStack;
                }

                fCashFlow += fCash;

                Console.WriteLine("{0,10}{1,60}{2,15}{3,15}{4,5}{5,10}{6,10}{7,10}{8,5}{9,15}{10,15}{11,15}",
                Encounter.Date,
                Encounter.Encounter,
                Encounter.ResultPred.ToString(),
                string.Format("{0:0.000}", fCash),
                Encounter.Stack,
                string.Format("{0:0.000}", Encounter.WinOdd),
                $"{nPercentage}%",
                bValue ? "VALUE" : "NO_VALUE",
                Encounter.Result,
                string.Format("{0:0.000}", fCashFlow),
                Encounter.Tipp,
                Encounter.fConfidence);

                ++nCount;
            }

            Console.WriteLine($"Sum encounter: {nCount} - correct: {nCorrect} - false: {nFalse}");
            Console.WriteLine($"Stack average: {(float)nStackAvg/ (float)nCount}");
            Console.WriteLine($"Odds average: {(float)fAvgOdds / (float)nCount}");
        }

        static void GetWithFilter(ref List<Prediction> MyList, string Tipp = "1", int Prediction = 50, int Stack = 1, float Odd = 1)
        {
            float cashFlow = 0;

            GetCashFlowZuluBet(MyList, null, null);


            //if (Tipp == "*")
            //{
            //    var Predictions = from Item in MyList
            //                  where
            //                  //Item.Tipp == Tipp &&
            //                  (Item.Tipp == "1") &&
            //                  //Item.Tipp == "1" &&
            //                  //Item.Stack >= Stack &&
            //                  Item.ResultPred != PredResult.NOT_PREDICTED
            //                  //orderby Item.ResultPred, Item.Stack
            //                  select Item;

            //    GetCashFlowPerDay(Predictions, ref fCash, Odd, Prediction);
            //}
            //else
            //{
            //    var Predictions = from Item in MyList
            //                  where
            //                  Item.Tipp == Tipp &&
            //                  //(Item.Tipp == "1X" || Item.Tipp == "X2" || Item.Tipp == "12") &&
            //                  //Item.Stack >= Stack &&
            //                  Item.ResultPred != PredResult.NOT_PREDICTED
            //                  //orderby Item.Date
            //                  select Item;

            //     GetCashFlowPerDay(Predictions, ref fCash, Odd, Prediction);
            //}
        }
    }
}
