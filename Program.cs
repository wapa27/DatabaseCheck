using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mail;
using henrySqlStuff;
using System.Net.Mime;

//DELETE
//ASK ABOUT RENAMING PROGS.

namespace bridgetechDatabaseCheck
{
  
    class Program
    {
        private static int intAppID = 1040;

        private static string strConnectMain = "Data Source=66.85.128.171;Initial Catalog=bridgemain;User ID=warren;Password=jl-a#1uKif?lrabrl?h@";


        // UNCOMMENT
        private static string strErrorFilePath = @"c:\\log\errorlog.txt";

        // DELETE


        // private static string strErrorFilePath = @"c:\\log\bridgetechDatabaseCheck\errorlog.txt";

        static DataTable dtModems;
        static DataTable dtFacilities;
        static DataTable dtRecipients;
        //static int intProcessedCount = 0;
        

             
        static void Main(string[] args)
        {
            start();
        }

        static void start()
        {

            try
            {


                Console.WriteLine("Building reports...");

                loadTables();

                loadRecipients();
                //  Console.Write(dtFacilities.Rows.Count.ToString());

                DateTime dtNow = DateTime.Now;

                DateTime dtStartDate = dtNow.Date.AddHours(-24);
                Int16 int16Year = System.Convert.ToInt16(dtStartDate.Year);
                Int16 int16Month = System.Convert.ToInt16(dtStartDate.Month);
                Int16 int16Day = System.Convert.ToInt16(dtStartDate.Day);
                DateTime dtEndDate = dtNow.Date;
                StringBuilder strbFacs = new StringBuilder();
                StringBuilder strPhxFac = new StringBuilder();


                /////WORKING
                strbFacs.AppendLine("<center><a href='http://btapi.net/reportUpdate.aspx?repID=" + 3 + "'  style='button'>Acknowledge Receipt of Report</a></center>");
                /////




                for (int intFacCnt = 0; intFacCnt < dtFacilities.Rows.Count; intFacCnt++)//each facility
                {
                    strbFacs.AppendLine("<br/>");
                    strbFacs.AppendLine("<p style=\"font-weight:bold; text-align:center; font-size:28px; font-color:black\">" + dtFacilities.Rows[intFacCnt]["FacilityName"].ToString() + "</br> " + "</p>" + "<p style=\"font-weight:semi-bold; text-align:center; font-size:25px; font-color:gray\">" + dtStartDate.ToString("D") + "</p>");
                    int intFacId = (int)dtFacilities.Rows[intFacCnt]["FacilityID"];

                    loadModemsTable(intFacId);

                    int[][] intArryModems = new int[dtModems.Rows.Count][];

                    StringBuilder strb = new StringBuilder();
                    int intOffSum = 0;
                    int intOnSum = 0;
                    int intTracker = 0;

                    // Begin html table for each facility. 
                    strb.AppendLine(addHTMLTableHeader());

                    for (int intModemCnt = 0; intModemCnt < dtModems.Rows.Count; intModemCnt++)//each modem
                    {

                        DataTable dtGpsCount = getGpsCount(dtFacilities.Rows[intFacCnt]["connectString"].ToString(), (int)dtModems.Rows[intModemCnt]["modemid"], dtStartDate, dtEndDate, intFacId);

                        if (dtGpsCount.Rows.Count != 0 && (int)dtGpsCount.Rows[0]["gpscount"] != 0)
                        {//if-1

                            ClassCounts scCounts = getModemCounts(dtFacilities.Rows[intFacCnt]["connectString"].ToString(),
                                (int)dtModems.Rows[intModemCnt]["modemid"], dtStartDate, dtEndDate, intFacId);

                            if (scCounts.intCounts[4] != -999 && scCounts.intCounts[5] != -999) //has PC data
                            {//if-2

                                if (scCounts.floatP > 95)//if-3 accuracy seems normal
                                {

                                    strb.AppendLine(addHTMLTableRowHasGoodData(intTracker, intModemCnt, dtGpsCount, scCounts, intFacId, int16Year, int16Month, int16Day));

                                }
                                else //if-3 accuracy isn't normal
                                {

                                    if (scCounts.intCounts[4] <= 100)//if-4 but there isn't enough data to be accurate.
                                    {

                                        strb.AppendLine(addHTMLTableRowHasGoodData(intTracker, intModemCnt, dtGpsCount, scCounts, intFacId, int16Year, int16Month, int16Day));

                                    }
                                    else//if-4 there IS enough data to be accurate but it still isn't accurate.
                                    {

                                        strb.AppendLine(addHTMLTableRowHasInnacurateData(intTracker, intModemCnt, dtGpsCount, scCounts, intFacId, int16Year, int16Month, int16Day));

                                    }//end if 4, non accurate
                                }//end if 3, non accurate

                                intArryModems[intModemCnt] = scCounts.intCounts;
                                intOffSum = intOffSum + scCounts.intCounts[4];
                                intOnSum = intOnSum + scCounts.intCounts[5];

                            }//if-2
                            else//has gps but no pc data
                            {

                                if ((int) dtGpsCount.Rows[0]["gpscount"] > 100 && scCounts.intCounts[5] == -999)

                                {
                                    strb.AppendLine(addHTMLTableRowHasGPSButNoPCData(intTracker, intModemCnt,
                                        dtGpsCount, scCounts,
                                        intFacId, int16Year, int16Month, int16Day));
                                }
                                else
                                {
                                    strb.AppendLine(addHTMLTableRowNoPCData(intTracker, intModemCnt, dtGpsCount,
                                        intFacId, int16Year, int16Month, int16Day));
                                }

                            }

                            var varFixstring = getModemFixstring(dtStartDate, dtEndDate, (int)dtModems.Rows[intModemCnt]["modemid"], dtFacilities.Rows[intFacCnt]["connectString"].ToString(), intFacId);

                            saveData(System.Convert.ToInt16(dtModems.Rows[intModemCnt]["modemid"]),
                                (int)dtGpsCount.Rows[0]["gpscount"], (Int16)scCounts.intCounts[0],
                                (Int16)scCounts.intCounts[2], (Int16)scCounts.intCounts[1],
                                (Int16)scCounts.intCounts[3], (Int16)scCounts.intCounts[4],
                                (Int16)scCounts.intCounts[5], scCounts.floatP, (Int16)scCounts.intCounts[6],
                                (Int16)scCounts.intCounts[8], (Int16)scCounts.intCounts[7],
                                (Int16)scCounts.intCounts[9], int16Year, int16Month, int16Day, dtFacilities.Rows[intFacCnt]["connectString"].ToString(), dtStartDate, varFixstring.intType32Count);
                            //string strModemFix = getModemFixstring(dtStartDate, dtEndDate, (int)dtModems.Rows[intModemCnt]["modemid"], dtFacilities.Rows[intFacCnt]["connectString"].ToString(), intFacId);


                            strb.Append(varFixstring.strT32HTML);


                        }//if-1 **********************************************************************************************
                        else
                        {//no gps count


                            var varFixstring = getModemFixstring(dtStartDate, dtEndDate, (int)dtModems.Rows[intModemCnt]["modemid"], dtFacilities.Rows[intFacCnt]["connectString"].ToString(), intFacId);

                            saveData(System.Convert.ToInt16(dtModems.Rows[intModemCnt]["modemid"]), 0,
                               0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, int16Year, int16Month, int16Day, dtFacilities.Rows[intFacCnt]["connectString"].ToString(), dtStartDate, varFixstring.intType32Count);


                            strb.AppendLine(addHTMLTableRowNoData(intTracker, intModemCnt, dtGpsCount, intFacId, int16Year, int16Month, int16Day));

                        }

                        intTracker++;
                    }//for modems

                    strb.AppendLine("</tbody></table></center>");

                    strbFacs.Append(strb);
                    if (intFacId == 8)
                    {
                        strPhxFac.Append(strb);
                    }
                    float fltPercentage;
                    if (intOffSum > intOnSum)
                    {
                        fltPercentage = ((float)intOnSum / (float)intOffSum) * 100;

                    }
                    else
                    {
                        fltPercentage = ((float)intOffSum / (float)intOnSum) * 100;

                    }
               
                    strbFacs.AppendLine(" <br><br><center><span style='font-weight:bold'><a href='http://btapi.net/berthDataSummary.aspx?facid=" + intFacId + "'>Summary</a><br>" +
                    "<br> Total Offs = " + intOffSum + "<br>" +
                    "Total Ons = " + intOnSum + "<br>");

                    if (fltPercentage > 94)//if overall accuracy greater than 94%, show in black.
                    {
                        strbFacs.AppendLine("Accuracy %: " + fltPercentage + "</span></center><br/>");
                    }
                    else//show in red
                    {
                        strbFacs.AppendLine("Accuracy %: <font color=\"red\">" + fltPercentage + "</font></span></center><br/>");
                    }

                    strbFacs.AppendLine("<br><br><hr><br>");

                    saveSummary(intOffSum, intOnSum, fltPercentage, dtFacilities.Rows[intFacCnt]["connectString"].ToString(), dtStartDate);
                }//for facilities



                string strReport = strbFacs.ToString();

                sendmail(strReport, dtNow);

                Console.WriteLine("Complete.");


            }
            catch (Exception e)
            {
                writeError(e);
            }

        }//******************************************************start*********************

        static private string addHTMLTableHeader()
        {
            string strHeader;

            strHeader =
                "<center><table cellpadding=\"3\" style =\"border-collapse:collapse; width:75%; empty-cells:hide\" border='1' font-color=\"black\">" +
                    "<thead>" +
                        "<tr>" +
                           "<th style=\"padding:10px\" scope = \"col\">Bus</th>" +
                           "<th  scope = \"col\">GPS Count</th>" +
                           "<th  scope = \"col\">Door 1 Off</th>" +
                           "<th  scope = \"col\">Door 1 On</th>" +
                           "<th  scope = \"col\">Door 2 Off</th>" +
                           "<th  scope = \"col\">Door 2 On</th>" +
                           "<th  scope = \"col\">Total Off</th>" +
                           "<th  scope = \"col\">Total On</th>" +
                           "<th  scope = \"col\">Ratio ON/OFF</th>" +
                           "<th  scope = \"col\">Door 1 Max Off</th>" +
                           "<th  scope = \"col\">Door 1 Max On</th>" +
                           "<th  scope = \"col\">Door 2 Max Off</th>" +
                           "<th  scope = \"col\">Door 2 Max On</th>" +
                           "<th  scope = \"col\">Time Errors</th>" +
                           "<th  scope = \"col\">Type 32 Count</th>" +
                        "</tr>" +
                    "</thead>" +
                    "<tbody>";

            return strHeader;
        }


        static private string addHTMLTableRowHasInnacurateData(int intTracker, int intModemCnt, DataTable dt, ClassCounts scCounts, int intFacId, int int16Year, int int16Month, int int16Day)
        {
            string strRow;

            strRow =
                        "<tr bgcolor=\"ffcccc\">" +
                        "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + int16Year + "&month=" + int16Month + "&day=" + int16Day + "'><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</strong></td>" + //bus
                        "<td align = \"center\">" + (int)dt.Rows[0]["gpscount"] + "</td>" + //gps count
                        "<td align = \"center\">" + scCounts.intCounts[0] + "</td>" + //door 1 off
                        "<td align = \"center\">" + scCounts.intCounts[2] + "</td>" + //door 1 on
                        "<td align = \"center\">" + scCounts.intCounts[1] + "</td>" + //door 2 off
                        "<td align = \"center\">" + scCounts.intCounts[3] + "</td>" + //door 2 on
                        "<td align = \"center\">" + scCounts.intCounts[4] + "</td>" + //total off
                        "<td align = \"center\">" + scCounts.intCounts[5] + "</td>" + //total on
                        "<td align = \"center\">" + scCounts.floatP + "</td>" + //accuracy
                        "<td align = \"center\">" + scCounts.intCounts[6] + "</td>" + //door 1 off max
                        "<td align = \"center\">" + scCounts.intCounts[8] + "</td>" + //door 1 on max
                        "<td align = \"center\">" + scCounts.intCounts[7] + "</td>" + //door 2 off max
                        "<td align = \"center\">" + scCounts.intCounts[9] + "</td>" + //door 2 on max
                        "<td align = \"center\">" + 0 + "</td>";  //time errors
            return strRow;
        }


        static private string addHTMLTableRowHasGoodData(int intTracker, int intModemCnt, DataTable dt, ClassCounts scCounts, int intFacId, int int16Year, int int16Month, int int16Day)
        {
            string strRow;

            if (intTracker % 2 == 0)
            {
                strRow =
                        "<tr bgcolor=\"eeeeee\">" +
                        "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + int16Year + "&month=" + int16Month + "&day=" + int16Day + "'><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</strong></td>" + //bus
                        "<td align = \"center\">" + (int)dt.Rows[0]["gpscount"] + "</td>" + //gps count
                        "<td align = \"center\">" + scCounts.intCounts[0] + "</td>" + //door 1 off
                        "<td align = \"center\">" + scCounts.intCounts[2] + "</td>" + //door 1 on
                        "<td align = \"center\">" + scCounts.intCounts[1] + "</td>" + //door 2 off
                        "<td align = \"center\">" + scCounts.intCounts[3] + "</td>" + //door 2 on
                        "<td align = \"center\">" + scCounts.intCounts[4] + "</td>" + //total off
                        "<td align = \"center\">" + scCounts.intCounts[5] + "</td>" + //total on
                        "<td align = \"center\">" + scCounts.floatP + "</td>" + //accuracy
                        "<td align = \"center\">" + scCounts.intCounts[6] + "</td>" + //door 1 off max
                        "<td align = \"center\">" + scCounts.intCounts[8] + "</td>" + //door 1 on max
                        "<td align = \"center\">" + scCounts.intCounts[7] + "</td>" + //door 2 off max
                        "<td align = \"center\">" + scCounts.intCounts[9] + "</td>" + //door 2 on max
                        "<td align = \"center\">" + 0 + "</td>";  //time errors                    
            }
            else
            {
                strRow =
                        "<tr>" +
                        "<td align = \"center\"><strong><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + int16Year + "&month=" + int16Month + "&day=" + int16Day + "'>" + dtModems.Rows[intModemCnt]["modemname"] + "</strong></td>" + //bus
                        "<td align = \"center\">" + (int)dt.Rows[0]["gpscount"] + "</td>" + //gps count
                        "<td align = \"center\">" + scCounts.intCounts[0] + "</td>" + //door 1 off
                        "<td align = \"center\">" + scCounts.intCounts[2] + "</td>" + //door 1 on
                        "<td align = \"center\">" + scCounts.intCounts[1] + "</td>" + //door 2 off
                        "<td align = \"center\">" + scCounts.intCounts[3] + "</td>" + //door 2 on
                        "<td align = \"center\">" + scCounts.intCounts[4] + "</td>" + //total off
                        "<td align = \"center\">" + scCounts.intCounts[5] + "</td>" + //total on
                        "<td align = \"center\">" + scCounts.floatP + "</td>" + //accuracy
                        "<td align = \"center\">" + scCounts.intCounts[6] + "</td>" + //door 1 off max
                        "<td align = \"center\">" + scCounts.intCounts[8] + "</td>" + //door 1 on max
                        "<td align = \"center\">" + scCounts.intCounts[7] + "</td>" + //door 2 off max
                        "<td align = \"center\">" + scCounts.intCounts[9] + "</td>" + //door 2 on max
                        "<td align = \"center\">" + 0 + "</td>";  //time errors  
            }

            return strRow;
        }


        static private string addHTMLTableRowNoData(int intTracker, int intModemCnt, DataTable dt, int intFacId, int int16Year, int int16Month, int int16Day)
        {
            string strRow;

            if (intTracker % 2 == 0)
            {
                strRow =
                         "<tr bgcolor=\"eeeeee\">" +
                              "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + int16Year + "&month=" + int16Month + "&day=" + int16Day + "'><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</strong></td>" + //bus
                              "<td align = \"center\">" + (int)dt.Rows[0]["gpscount"] + "</td>" + //gps count
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 off
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 on
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 off
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 on
                              "<td align = \"center\">" + 0 + "</td>" + //total off
                              "<td align = \"center\">" + 0 + "</td>" + //total on
                              "<td align = \"center\">" + 0 + "</td>" + //accuracy
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 off max
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 on max
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 off max
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 on max
                              "<td align = \"center\">" + 0 + "</td>" + //time errors
                              "<td align = \"center\">" + 0 + "</td>" + //type 32
                         "</tr>";
            }
            else
            {
                strRow =
                         "<tr>" +
                              "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + int16Year + "&month=" + int16Month + "&day=" + int16Day + "'><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</strong></td>" + //bus
                              "<td align = \"center\">" + (int)dt.Rows[0]["gpscount"] + "</td>" + //gps count
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 off
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 on
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 off
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 on
                              "<td align = \"center\">" + 0 + "</td>" + //total off
                              "<td align = \"center\">" + 0 + "</td>" + //total on
                              "<td align = \"center\">" + 0 + "</td>" + //accuracy
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 off max
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 on max
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 off max
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 on max
                              "<td align = \"center\">" + 0 + "</td>" + //time errors
                              "<td align = \"center\">" + 0 + "</td>" + //type 32
                         "</tr>";
            }

            return strRow;
        }


        static private string addHTMLTableRowNoPCData(int intTracker, int intModemCnt, DataTable dt, int intFacId, int int16Year, int int16Month, int int16Day)
        {
            string strRow;

            if (intTracker % 2 == 0)
            {
                strRow =
                         "<tr bgcolor=\"eeeeee\">" +
                              "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + int16Year + "&month=" + int16Month + "&day=" + int16Day + "'><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</strong></td>" + //bus
                              "<td align = \"center\">" + (int)dt.Rows[0]["gpscount"] + "</td>" + //gps count
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 off
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 on
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 off
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 on
                              "<td align = \"center\">" + 0 + "</td>" + //total off
                              "<td align = \"center\">" + 0 + "</td>" + //total on
                              "<td align = \"center\">" + 0 + "</td>" + //accuracy
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 off max
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 on max
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 off max
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 on max
                              "<td align = \"center\">" + 0 + "</td>"; //time errors
            }
            else
            {
                strRow =
                         "<tr>" +
                              "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + int16Year + "&month=" + int16Month + "&day=" + int16Day + "'><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</strong></td>" + //bus
                              "<td align = \"center\">" + (int)dt.Rows[0]["gpscount"] + "</td>" + //gps count
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 off
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 on
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 off
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 on
                              "<td align = \"center\">" + 0 + "</td>" + //total off
                              "<td align = \"center\">" + 0 + "</td>" + //total on
                              "<td align = \"center\">" + 0 + "</td>" + //accuracy
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 off max
                              "<td align = \"center\">" + 0 + "</td>" + //door 1 on max
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 off max
                              "<td align = \"center\">" + 0 + "</td>" + //door 2 on max
                              "<td align = \"center\">" + 0 + "</td>"; //time errors
            }

            return strRow;
        }
        static private string addHTMLTableRowHasGPSButNoPCData(int intTracker, int intModemCnt, DataTable dt, ClassCounts scCounts, int intFacId, int int16Year, int int16Month, int int16Day)
        {
            string strRow = "";

            strRow =
                "<tr bgcolor=\"ff4d4d\">" +
                "<td align = \"center\"><strong><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + int16Year + "&month=" + int16Month + "&day=" + int16Day + "'>" + dtModems.Rows[intModemCnt]["modemname"] + "</strong></td>" + //bus
                "<td align = \"center\">" + (int)dt.Rows[0]["gpscount"] + "</td>" + //gps count
                "<td align = \"center\">" + 0 + "</td>" + //door 1 off
                "<td align = \"center\">" + 0 + "</td>" + //door 1 on
                "<td align = \"center\">" + 0 + "</td>" + //door 2 off
                "<td align = \"center\">" + 0 + "</td>" + //door 2 on
                "<td align = \"center\">" + 0 + "</td>" + //total off
                "<td align = \"center\">" + 0 + "</td>" + //total on
                "<td align = \"center\">" + 0 + "</td>" + //accuracy
                "<td align = \"center\">" + 0 + "</td>" + //door 1 off max
                "<td align = \"center\">" + 0 + "</td>" + //door 1 on max
                "<td align = \"center\">" + 0 + "</td>" + //door 2 off max
                "<td align = \"center\">" + 0 + "</td>" + //door 2 on max
                "<td align = \"center\">" + 0 + "</td>";  //time errors  



            return strRow;
        }






        static private void saveData(Int16 int16Modemid, int intGpsCount, Int16 int16d1off, Int16 int16d1ons, Int16 int16d2off, Int16 int16d2ons, Int16 int16TotalOffs, Int16 int16TotalOns, float fltRatio, Int16 int16d1MaxOffs, Int16 int16d1MaxOns, Int16 int16d2MaxOffs, Int16 int16d2MaxOns, Int16 int16Year, Int16 int16Month, Int16 intDay, string strConnect, DateTime dtnow,int intModemFix)
        {


            string strMerge = "MERGE tblDailyBerthData USING ( VALUES('" + int16Modemid + "','" + intGpsCount + "','" + int16d1off +
                              "','" + int16d1ons + "','" + int16d2off + "','" + int16d2ons + "','" + int16TotalOffs + "','" + int16TotalOns + "','" + fltRatio + "','" + int16d1MaxOffs + "','" + int16d1MaxOns + "','" + int16d2MaxOffs + "','" + int16d2MaxOns + "','" + int16Year + "','" + int16Month + "','" + intDay + "','" + dtnow +"','"+intModemFix+
                              "')) as Source([modemid] ,[gpscount] ,[d1off] ,[d1ons] ,[d2off] ,[d2ons] ,[totaloffs] ,[totalons] ,[ratio1] ,[d1maxoff] ,[d1maxon] ,[d2maxoff] ,[d2maxon] ,[year] ,[month] ,[day] ,[timestamp],type32Count) ON (tblDailyBerthData.modemid = source.modemid and tblDailyBerthData.year = source.year and tblDailyBerthData.month = source.month and tblDailyBerthData.day = source.day ) WHEN NOT MATCHED BY TARGET THEN INSERT ([modemid] ,[gpscount] ,[d1off] ,[d1ons] ,[d2off] ,[d2ons] ,[totaloffs] ,[totalons] ,[ratio1] ,[d1maxoff] ,[d1maxon] ,[d2maxoff] ,[d2maxon] ,[year] ,[month] ,[day] ,[timestamp],type32count) values(Source.[modemid] ,Source.[gpscount] ,Source.[d1off] ,Source.[d1ons] ,Source.[d2off] ,Source.[d2ons] ,Source.[totaloffs] ,Source.[totalons] ,Source.[ratio1] ,Source.[d1maxoff] ,Source.[d1maxon] ,Source.[d2maxoff] ,Source.[d2maxon] ,Source.[year] ,Source.[month] ,Source.[day] ,Source.[timestamp],Source.type32Count) output $action;";
            henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, strMerge, strErrorFilePath);

        }
        static private void saveSummary(int intSumoff, int intSumOn, float fltPercentage, string strFacConnect, DateTime dtNow)
        {
            // VALUES('" + int16Modemid + "','" + intGpsCount + "','" 

            string strMerge =
                "MERGE tblDailyFacilityTotal USING ( VALUES('" + intSumoff + "','" + intSumOn + "','" + fltPercentage + "','" + dtNow + "')) as Source(totalOffs,totalOns,accuracy,timestamp) ON (Source.totalOffs = tblDailyFacilityTotal.totalOffs and Source.totalOns = tblDailyFacilityTotal.totalOns and Source.accuracy = tblDailyFacilityTotal.accuracy and Source.timestamp = tblDailyFacilityTotal.timestamp) WHEN NOT MATCHED BY TARGET THEN INSERT (totalOffs,totalOns,accuracy,timestamp) VALUES(Source.totalOffs,Source.totalOns,Source.accuracy,Source.timestamp) output $action,inserted.id;";

            henrySqlStuff.execute.sqlExecuteSelectForever(strFacConnect, strMerge, strErrorFilePath);


        }
        //  static void start()
        //  {
        //      loadTables();
        //      //  Console.Write(dtFacilities.Rows.Count.ToString());

        //      loadRecipients();

        //      DateTime dtNow = DateTime.Now;

        //      DateTime dtStartDate = DateTime.UtcNow.AddHours(-24);
        //      DateTime dtEndDate =DateTime.UtcNow;
        //      StringBuilder strbFacs = new StringBuilder();
        //      StringBuilder strPhxFac= new StringBuilder();


        //      for (int intFacCnt = 0; intFacCnt < dtFacilities.Rows.Count; intFacCnt++)//each facility
        //      {
        //          //DELETE AFTER ASKING ABOUT PHOENIX GCM
        //          if (intFacCnt != 4)//need to remove, PhoenixGCM is throwing error because missing PeopleCnt2 table...
        //          {
        //              strbFacs.AppendLine("<br/>");
        //              strbFacs.AppendLine("<center><font size=\"6\"><strong>" + dtFacilities.Rows[intFacCnt]["FacilityName"].ToString() + "</strong></font></center><br/>");
        //              int intFacId = (int)dtFacilities.Rows[intFacCnt]["FacilityID"];

        //              //need to ask about this section
        //              if (intFacId == 8)
        //              {
        //                  strPhxFac.AppendLine("<br/>");
        //                  strPhxFac.AppendLine("<center><font size=\"6\"><strong>" + dtFacilities.Rows[intFacCnt]["FacilityName"].ToString() + "</strong></font></center><br/>");
        //              }

        //              loadModemsTable(intFacId);

        //              int[][] intArryModems = new int[dtModems.Rows.Count][];

        //              StringBuilder strb = new StringBuilder();
        //              int intOffSum = 0;
        //              int intOnSum = 0;
        //              int intTracker = 0;

        //              //Begin html table for each facility.
        //              strb.AppendLine("<center><table cellpadding=\"3\" style =\"border-collapse:collapse; width:75%; empty-cells:hide\" border='1' font-color=\"black\">" +
        //                  "<thead>" +
        //                      "<tr>" +
        //                         "<th style=\"padding:10px\" scope = \"col\">Name</th>" +
        //                         "<th  scope = \"col\">Off</th>" +
        //                         "<th  scope = \"col\">On</th>" +
        //                         "<th  scope = \"col\">Time Errors</th>" +
        //                         "<th  scope = \"col\">Type 32</th>" +
        //                      "</tr>" +
        //                  "</thead>" +
        //                  "<tbody>"
        //                  );

        //              for (int intModemCnt = 0; intModemCnt < dtModems.Rows.Count; intModemCnt++)//each modem
        //              {
        //                  DataTable dt = getGpsCount(dtFacilities.Rows[intFacCnt]["connectString"].ToString(), (int)dtModems.Rows[intModemCnt]["modemid"], dtStartDate, dtEndDate, intFacId);
        //                  Boolean bolRowWithNoData = false;
        //                  int[] intArryModemCount = getModemCounts(dtFacilities.Rows[intFacCnt]["connectString"].ToString(), (int)dtModems.Rows[intModemCnt]["modemid"], dtStartDate, dtEndDate,intFacId);
        //                  if (intArryModemCount[0] != -999 && intArryModemCount[1] != -999)//has data
        //                  {
        //                      int intDTErrors = getDateTimeErrors(dtStartDate, dtEndDate,
        //                          (int)dtModems.Rows[intModemCnt]["modemid"],
        //                          dtFacilities.Rows[intFacCnt]["connectString"].ToString());

        //                      //add table row with data
        //                      strb.AppendLine(addHTMLRowData(intTracker, dtModems, intModemCnt, intDTErrors, intArryModemCount));

        //                      intArryModems[intModemCnt] = intArryModemCount;
        //                      intOffSum = intOffSum + intArryModemCount[0];
        //                      intOnSum = intOnSum + intArryModemCount[1];

        //                  }
        //                  else
        //                  {
        //                      //add table row no data
        //                      bolRowWithNoData = true;
        //                      strb.AppendLine(addHTMLRowNoData(intTracker, intModemCnt, dtModems));

        //                  }

        //                  string strModemFix = getModemFixstring(dtStartDate, dtEndDate, (int)dtModems.Rows[intModemCnt]["modemid"],
        //                       dtFacilities.Rows[intFacCnt]["connectString"].ToString(), bolRowWithNoData);

        //                  strb.Append("<td align = \"center\">" + strModemFix + "</td></tr></tbody>");

        //                  intTracker++;

        //              }//for modems


        //              strbFacs.Append(strb);

        //              if (intFacId == 8)
        //              {
        //                  strPhxFac.Append(strb);
        //              }

        //              float fltPercentage;

        //              if (intOffSum > intOnSum)
        //              {
        //                  fltPercentage = ((float)intOnSum / (float)intOffSum) * 100;
        //              }
        //              else
        //              {
        //                  fltPercentage = ((float)intOffSum / (float)intOnSum) * 100;
        //              }

        //              strbFacs.AppendLine("</table><br><br><span style='font-weight:bold'><u>Summary</u><br><br>Total Offs: " + intOffSum + "<br>Total Ons: " + intOnSum);// + "<br>Accuracy: " + fltPercentage + "</span></center><br/><br><hr>");

        //              if(fltPercentage < 94)
        //              {
        //                  strbFacs.AppendLine("<br>Accuracy: <font color = \"red\">" + fltPercentage + "</font></span></center><br/><br><hr>");
        //              }
        //              else
        //              {
        //                  strbFacs.AppendLine("<br>Accuracy: " + fltPercentage + "</span></center><br/><br><hr>");
        //              }

        //              //NEED TO ASK
        //              if (intFacId == 8)
        //              {
        //                  strPhxFac.AppendLine("</table><span style='font-weight:bold'>Tot. offs=" + intOffSum + " Tot.ons=" + intOnSum + " %:" + fltPercentage + "</span></center><br/><br><hr>");
        //              }

        //          }
        //      }//for facilities


        //      string strReport = strbFacs.ToString();
        //      string strReportPhx = strPhxFac.ToString();

        //      // UNCOMMENT
        //      //sendmail("henry@arizona-networks.com", strReport);

        //      // DELETE
        //      sendmail(strReport, dtNow);

        ////  sendmailPhx("henry@bridgetech.net", strReportPhx);


        //  }//*************************start*********************


        static private ClassCounts getModemCounts(string strFacConnect,int intModemId,DateTime dtBegin,DateTime dtEndTime,int intFacId)
        {
            ClassCounts scAnswer = new ClassCounts();
            try
            {


                DataTable dtModemPCCountSummary = getModemDataSummary(dtBegin, dtEndTime, intModemId, strFacConnect, intFacId);

                if (dtModemPCCountSummary.Rows.Count > 0)
                {
                    Int16 intD1OffCount = 0;
                    Int16 intD2OffCount = 0;
                    Int16 intD1OnCount = 0;
                    Int16 intD2OnCount = 0;
                    Int16 intOffsTotal = 0;
                    Int16 intOnsTotal = 0;
                    Int16 intD1MaxOffs = 0;
                    Int16 intD2MaxOffs = 0;
                    Int16 intD1MaxOns = 0;
                    Int16 intD2MaxOns = 0;

                    if (!dtModemPCCountSummary.Rows[0].IsNull("D1offs"))
                    {
                        intD1OffCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["D1offs"]);
                    }
                    else
                    {
                        intD1OffCount = -999;
                    }

                    if (!dtModemPCCountSummary.Rows[0].IsNull("D2offs"))
                    {
                        intD2OffCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["D2offs"]);
                    }
                    else
                    {
                        intD2OffCount = -999;
                    }
                    if (!dtModemPCCountSummary.Rows[0].IsNull("D1ons"))
                    {
                        intD1OnCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["D1ons"]);
                    }
                    else
                    {
                        intD1OnCount = -999;
                    }
                    if (!dtModemPCCountSummary.Rows[0].IsNull("D2ons"))
                    {
                        intD2OnCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["D2ons"]);
                    }
                    else
                    {
                        intD2OnCount = -999;
                    }
                    if (!dtModemPCCountSummary.Rows[0].IsNull("m1off"))
                    {
                        intD1MaxOffs = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m1off"]);
                    }
                    else
                    {
                        intD1MaxOffs = -999;
                    }
                    if (!dtModemPCCountSummary.Rows[0].IsNull("m2off"))
                    {
                        intD2MaxOffs = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m2off"]);
                    }
                    else
                    {
                        intD2MaxOffs = -999;
                    }
                    if (!dtModemPCCountSummary.Rows[0].IsNull("m1on"))
                    {
                        intD1MaxOns = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m1on"]);
                    }
                    else
                    {
                        intD1MaxOns = -999;
                    }
                    if (!dtModemPCCountSummary.Rows[0].IsNull("m2on"))
                    {
                        intD2MaxOns = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m2on"]);
                    }
                    else
                    {
                        intD2MaxOns = -999;
                    }
                    if (intD2OffCount != -999 && intD1OffCount != -999)
                    {
                        intOffsTotal = System.Convert.ToInt16(intD1OffCount + intD2OffCount);
                    }
                    else
                    {
                        intOffsTotal = -999;
                    }
                    if (intD2OnCount != -999 && intD1OnCount != -999)
                    {
                        intOnsTotal = System.Convert.ToInt16(intD1OnCount + intD2OnCount);

                    }
                    else
                    {
                        intOnsTotal = -999;
                    }

                    float fltPercentage = 0;
                    if (intOffsTotal != -999 && intOnsTotal != -999 &&intOffsTotal !=0)
                    {
                        if (intOnsTotal > intOffsTotal)
                        {
                            fltPercentage = intOnsTotal / System.Convert.ToSingle(  intOffsTotal);
                        }
                        else
                        {
                            fltPercentage = (intOffsTotal / System.Convert.ToSingle(intOnsTotal))*-1;
                        }
                    }



                    scAnswer.intCounts[0] = intD1OffCount;
                    scAnswer.intCounts[1] = intD2OffCount;
                    scAnswer.intCounts[2] = intD1OnCount;
                    scAnswer.intCounts[3] = intD2OnCount;
                    scAnswer.intCounts[4] = intOffsTotal;
                    scAnswer.intCounts[5] = intOnsTotal;
                    scAnswer.intCounts[6] = intD1MaxOffs;
                    scAnswer.intCounts[7] = intD2MaxOffs;
                    scAnswer.intCounts[8] = intD1MaxOns;
                    scAnswer.intCounts[9] = intD2MaxOns;


                    //   scAnswer.intCounts = {intD1OffCount,intD2OffCount,intD1OnCount,intD2OnCount,intOffsTotal,intOnsTotal}


                    scAnswer.floatP = fltPercentage;
                }//if rows > 0



            }
            catch (Exception e)
            {
                writeError(e);
            }
            return scAnswer;
        }


        static private int getDateTimeErrors(DateTime dtBegin, DateTime dtEnd, int intModemId, string strConnect)
        {
            int intAnswer = 0;
            string strSelect = "SELECT count([gpsid]) FROM [dbo].[tblGpsData] where gpsdatetime between  '" + dtBegin +
                               "' and '" + dtEnd + "' and datediff(minute,gpsdatetime,receiveddatetime)< 100";
            DataTable dt = henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, strSelect, @"c:\log\errorlog.txt");
            if(dt.Rows.Count > 1)
            {

                intAnswer = dt.Rows.Count;
            }
            return intAnswer;
        }


        static private DataTable getModemDataSummary(DateTime dtBegin, DateTime dtEnd, int intModemId, string strConnect, int intFacId)
        {
            string strSelect = "";
            DataTable dt = null;
            if (intFacId != 3 && intFacId != 14)
            {

                strSelect =
                    "select sum(d1off) as d1offs,sum(d2off) as d2offs,sum(d1on) as d1ons,sum(d2on) as d2ons,max(d1off) as m1off,max(d2off) as m2off,max(d1on) as m1on,max(d2on) as m2on  FROM [dbo].[tblPeopleCount2] where modemid =" +
                    intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEnd + "'";
                dt = henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, strSelect, @"c:\log\errorlog.txt");
            }
            else
            {
                strSelect =
                    "select sum(d1off) as d1offs,sum(d2off) as d2offs,sum(d1on) as d1ons,sum(d2on) as d2ons,max(d1off) as m1off,max(d2off) as m2off,max(d1on) as m1on,max(d2on) as m2on  FROM [dbo].[tblPeopleCount2V2] where modemid =" +
                    intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEnd + "'";
                dt = henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, strSelect, @"c:\log\errorlog.txt");
            }
            return dt;
        }



        static private (int intType32Count, string strT32HTML) getModemFixstring(DateTime dtBegin, DateTime dtEnd, int intModemId, string strConnect, int intFacId)
        {
            StringBuilder strAnswer = new StringBuilder();
            int intCount = 0;

            DataTable dt = getModemFix(strConnect, intModemId, dtBegin, dtEnd, intFacId);
            int intRows = dt.Rows.Count;
            Boolean bolHadType32 = false;
            for (int intCnt = 0; intCnt < intRows; intCnt++)
            {
                intCount = (int)dt.Rows[intCnt]["count"];
                string strStatus = dt.Rows[intCnt]["fixstatus"].ToString();

                //strAnswer.Append("<strong>Count: </strong>" + strCount + ", <strong>Fix Status: </strong>" + strStatus + "<br/>");

                if (strStatus == "32") //for now, only record type 32 fix status counts.
                {
                    bolHadType32 = true;
                    strAnswer.Append(
                        "<td align = \"center\">" + intCount + "</td>" + //type32 fix status count
                        "</tr>"
                    );
                }

            }

            if (bolHadType32 == false)
            {
                strAnswer.Append(
                    "<td align = \"center\">" + 0 + "</td>" + //type32 fix status count
                    "</tr>"
                );
            }

            return (intCount, strAnswer.ToString());
        }


        static private DataTable getModemFix(string strFacConnect, int intModemId, DateTime dtBegin, DateTime dtEndTime, int intFacId)
        {
            string strSelect = "";
            DataTable dt = null;
            if (intFacId != 3 && intFacId != 14)
            {
                strSelect = "SELECT count(gpsid) as count,fixstatus FROM [dbo].[tblGpsData] where modemid =" +
                            intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEndTime +
                            "' and fixstatus <> 0 group by fixstatus";
                dt = henrySqlStuff.execute.sqlExecuteSelectForever(strFacConnect, strSelect, @"c:\log\errorlog.txt");
            }
            else
            {
                strSelect = "SELECT count(gpsid) as count,fixstatus FROM [dbo].[tblGpsDataV2] where modemid =" +
                            intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEndTime +
                            "' and fixstatus <> 0 group by fixstatus";
                dt = henrySqlStuff.execute.sqlExecuteSelectForever(strFacConnect, strSelect, @"c:\log\errorlog.txt");
            }
            return dt;
        }



        static private void loadTables()
        {
            //select only type 1 facilities
            string strSelectMain =
                "select [FacilityID],[FacilityName],[FacilityCode],[Active],[TimeZoneID],[createdDate],[FacilityTypeID],[connectString],[honorDST],[nwLat],[nwLong],[seLat],[seLong] from tblFacilities where active = 1 and facilityid >2 and FacilityTypeID = 2";
            dtFacilities = henrySqlStuff.execute.sqlExecuteSelectForever(strConnectMain, strSelectMain, strErrorFilePath);
        
        
       
        }


        static private void loadModemsTable(int intFacId)
        {
            string strSelectModem = "SELECT [ModemID] ,[FacilityID] ,[ModemName] ,[IPAddress] ,[Active] ,[ESN] ,[MACAddress] ,[doorDirectionType],doorTypes,outSvc,outSvcWarn FROM [BridgeMain].[dbo].[Modem] where active = 1 and FacilityId ='" + intFacId + "' and active = '1'";
            dtModems = henrySqlStuff.execute.sqlExecuteSelectForever(strConnectMain, strSelectModem, strErrorFilePath);

        }//*****loadModemsTables

        //NEED TO MOVE TABLES TO BRIDGE MAIN
        static private void loadRecipients()
        {
            string strSelectRecipients =
                "SELECT enAssign.[appID], enAssign.[emailID],enAddress.emailAddress FROM [BridgeMain].[dbo].[tblEmailNotificationAssignments] enAssign inner join tblEmailNotificationAddresses enAddress on enAssign.emailID = enAddress.emailID and appid ='" + intAppID + "'";
            dtRecipients = henrySqlStuff.execute.sqlExecuteSelectForever(strConnectMain, strSelectRecipients, strErrorFilePath);
        }


        //static protected void sendmail(string emailaddress,  string strMessage)
        //{
        //    MailAddress from = new MailAddress("info@btapi.net");
        //    MailAddress to = new MailAddress(emailaddress);

        //    //    MailAddress ian = new MailAddress("ian@bridgetech.net");
        //    MailMessage msgContact = new MailMessage(from, to)
        //    {
        //        IsBodyHtml = true,
        //        Subject = "Database Check",
        //        //Body = strMessage
        //    };

        //    Attachment inlineLogo = new Attachment(@"C:\Users\mwill\Desktop\bridge.png");
        //    msgContact.Attachments.Add(inlineLogo);
        //    string contentID = "Image";
        //    inlineLogo.ContentId = contentID;
        //    inlineLogo.ContentDisposition.Inline = true;
        //    inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
        //    msgContact.Body = "<center><img src =\"cid:" + contentID + "\"></center><br/><br/>" + strMessage;

        // //  msgContact.CC.Add(ian);
        //    SmtpClient client = new SmtpClient("mail.btapi.net");
        //    client.Credentials = new System.Net.NetworkCredential("info@btapi.net", "sinjin26@");
        //    client.Port = 8889;

        //    try
        //    {
        //        client.Send(msgContact);
        //    }
        //    catch (Exception exc)
        //    {


        //    }
        //    msgContact.Dispose();
        //} //


        static protected void sendmail(string strMessage, DateTime dtNow)
        {


            try
            {


                Console.WriteLine("Sending reports...");
                int intRepID;
            //    intRepID = GetNextReportID();
            //    intRepID++;
                string strAcknowledgeButton;


                MailAddress from = new MailAddress("admin@bridgetech.net");
                //MailAddress to = new MailAddress("warren@bridgetech.net");
                //MailAddress ian = new MailAddress("ian@bridgetech.net");

                //NEED TO ADD HENRY AND IAN TO TABLE THAT HAS LIST OF EMAIL RECIPIENTS
                for (int intRecipientCount = 0; intRecipientCount < dtRecipients.Rows.Count; intRecipientCount++)//for each recipient
                {

                    //strAcknowledgeButton = "<center><button><a style=\"text-decoration:none;\" href='http://btapi.net/reportUpdate.aspx?repID=" + intRepID + "&email=" + dtRecipients.Rows[intRecipientCount]["emailAddress"].ToString() + "&report=Daily%20Bus%20Report'>Acknowledge Receipt of Report #" + intRepID + "</a></button></center>";

                    MailAddress to = new MailAddress(dtRecipients.Rows[intRecipientCount]["emailAddress"].ToString());

                    MailMessage msgContact = new MailMessage(from, to)
                    {
                        IsBodyHtml = true,
                        Subject = "Database Check Report #"// + intRepID,
                        // Body = strMessage
                    };

                    //Add logo to top of email
                    /*   Attachment inlineLogo = new Attachment("bridge.png");
                        msgContact.Attachments.Add(inlineLogo);
                         string contentID = "Image";
                         inlineLogo.ContentId = contentID;
                         inlineLogo.ContentDisposition.Inline = true;
                         inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;

                         msgContact.Body = "<center><img src =\"cid:" + contentID + "\"></center><br/><br/>" + strAcknowledgeButton + strMessage;
                         */
                    msgContact.Body = "<center></center><br/><br/>" + strMessage;
                    SmtpClient client = new SmtpClient("smtp.office365.com");
                    client.Credentials = new System.Net.NetworkCredential("admin@bridgetech.net", "Password101!");
                    client.EnableSsl = true;
                    client.Port = 587;
                    try
                    {
                        client.Send(msgContact);
                    }
                    catch (Exception exc)
                    {
                        writeError(exc);
                    }
                    msgContact.Dispose();

                    //string strUpdateRecLog = "INSERT INTO tblEmailReportLog VALUES('" + intRepID + "', '" + dtRecipients.Rows[intRecipientCount]["emailAddress"].ToString() + "', '" + dtNow.Date.ToString("d") + "', '" + dtNow.ToShortTimeString() + "', '" + "Daily Bus Report', " + 0 + ", " + 0 + ")";
                  //  henrySqlStuff.execute.sqlExecuteSelectForever(strConnectMain, strUpdateRecLog, strErrorFilePath);

                }
            }

            catch (Exception e)
            {
                writeError(e);
            }


        }//****sendmail****


        protected static void writeError(string strError, string strFunction, string strFile)
        {
            using (StreamWriter file = new StreamWriter(strFile, true))
            {
                file.WriteLine(DateTime.Now.ToString());
                file.WriteLine(strError);
                file.WriteLine(strFunction);
                file.WriteLine("------------------------------------------------------------");
                file.Close();
            }

        }
        static private DataTable getGpsCount(string strFacConnect, int intModemId, DateTime dtBegin, DateTime dtEndTime, int intFacId)
        {
            string strSelect = "";
            DataTable dt = null;

            if (intFacId != 3 && intFacId != 14)
            {

                strSelect = "SELECT count(gpsid) as gpsCount FROM [dbo].[tblGpsData] where modemid =" + intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEndTime + "'";
                dt = henrySqlStuff.execute.sqlExecuteSelectForever(strFacConnect, strSelect, @"c:\log\errorlog.txt");
            }
            else
            {
                strSelect = "SELECT count(gpsid) as gpsCount FROM [dbo].[tblGpsDatav2] where modemid =" + intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEndTime + "'";
                dt = henrySqlStuff.execute.sqlExecuteSelectForever(strFacConnect, strSelect, @"c:\log\errorlog.txt");
            }
            return dt;


        }

        static private string addHTMLRowData(int intTracker, DataTable dtModems, int intModemCnt, int intDTErrors, int[] intArryModemCount)
        {
            string strRow;

            if (intTracker % 2 == 0)
            {
                strRow =
                         "<tr bgcolor=\"eeeeee\">" +
                            "<td align = \"center\"><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</td>" + //name
                            "<td align = \"center\">" + intArryModemCount[0] + "</td>" + //offs
                            "<td align = \"center\">" + intArryModemCount[1] + "</td>" + //ons
                            "<td align = \"center\">" + intDTErrors + "</td>"; //time errors
            }
            else
            {
                strRow =
                         "<tr>" +
                            "<td align = \"center\"><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</td>" + //name
                            "<td align = \"center\">" + intArryModemCount[0] + "</td>" + //offs
                            "<td align = \"center\">" + intArryModemCount[1] + "</td>" + //ons
                            "<td align = \"center\">" + intDTErrors + "</td>"; //time errors
            }
            return strRow;
        }


        static private string addHTMLRowNoData(int intTracker, int intModemCnt, DataTable dtModems)
        {

            string strRow;

                strRow =
                            "<tr bgcolor=\"ffcccc\">" +
                                "<td align = \"center\"><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</td>" + //name
                                "<td align = \"center\"><font color=\"red\"></font></td>" + //offs
                                "<td align = \"center\"><font color=\"red\"></font></td>" + //ons
                                "<td align = \"center\"><font color=\"red\"></font></td>";  //time errors

            return strRow;

        }
        static private void writeError(Exception exc)
        {
            string strInnerMessage = "";
            Exception _exc1 = new Exception();
            using (StreamWriter w = File.AppendText(strErrorFilePath))
            {
                w.Write("\r\nLog Entry : ");
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                w.WriteLine(" :");
                w.WriteLine(" :{0}", exc.Message);
                if (_exc1.InnerException != null)
                {
                    strInnerMessage = _exc1.InnerException.Message;
                }
                w.WriteLine(" :{0}", strInnerMessage);
                w.WriteLine(" :{0}", exc.StackTrace);
                w.WriteLine(" :{0}", exc.Source);
                w.WriteLine(" :{0}", exc.TargetSite.Name.ToString());

                w.WriteLine("-------------------------------");
            }
        }//**writeError**

    }
    public class ClassCounts
    {
        public int[] intCounts = new int[10];
        public float floatP;
    }
}
