﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace BTHistoryReader
{
    public partial class ScatterForm : Form
    {

        List<cSeriesData> ThisSeriesData;
        static Random rnd;
        private string SeriesName = "";
        private double dBig = -1;
        private List<int> WUsPerSelection = new List<int>();
        int nWUtotal = 0;
        double dSmall = 1e6;
        private string strSeries = "X-Axis: Elapsed Time in ";
        // note to myself, elspased time is always in minutes
        private int CurrentNumberSeriesDisplayable = 0; // number available to view
        private int CurrentSeriesDisplayed = -1;        // the one being shown IFF only one series is shown else -1
        private bool bScatteringApps;
        private bool bShowSystemData;         // scattering systems
        private bool bShowDatasests;
        private bool bScatteringGPUs;
        double fScaleMultiplier;
        string strScaleUnits;
        bool bSeeError;
        double dScaledOffset = 0.0;     // change offset to minutes or hours as necessary.  default is seconds
        double dOrigOffset = 0.0;
        
        
        private MarkerStyle[] MarkerStyles = new MarkerStyle[] { MarkerStyle.None , MarkerStyle.Circle, MarkerStyle.Cross, MarkerStyle.Diamond, MarkerStyle.Square, MarkerStyle.Triangle, MarkerStyle.Star10, MarkerStyle.Star4, MarkerStyle.Star5, MarkerStyle.Star6};

        private MarkerStyle GetMS(int n)
        {
            int i = (n % (MarkerStyles.Length + 1));
            return MarkerStyles[i];
            /*
            if (n >= MarkerStyles.Length)
            {
                return MarkerStyle.None;
            }
            return MarkerStyles[n];
            */
        }
        private class cSaveOutlier
        {
            public string seriesname;
            public int iWhereRemoved;   // where point on screen was declared an outlier
            public int iWhereData;      // where data was declared an outlier
            public double fmpy;         // the restore multiplier
            public Color c;             // save original color
        }
        private Stack<cSaveOutlier> UsedOutliers;

        private double DistanceTo(Point point1, Point point2)
        {
            var a = (double)(point2.X - point1.X);
            var b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }




        // https://stackoverflow.com/questions/56484451/unexpected-side-effect-setting-point-colors-in-chart-xaxis
        // solution was found:  use color.transparent in addition to point.isempty and
        // do not restore color of points that have been hidden
        // currently nConcurrent is not used locally as it was applied before the call
        // offset can be used to offset each graph for visibility.  Applies to GPUO data records.  Scale is minutes for doffset
        public ScatterForm(ref List<cSeriesData> refSD, string WhatsShowing, bool bAllowSeeError, string strFilter, double dOffset)
        {
            InitializeComponent();
            bSeeError = bAllowSeeError;
            btnInvSel.Visible = bSeeError; // 2-3-2020 invert seletion requires all data for some bug I must have TODO FIXME
            lbAdvFilter.Text = strFilter;
            dOrigOffset = dOffset;
            cbUseOffset.Checked = false;
            cbUseOffset.Enabled = false;
            lbOffsetValue.Visible = false;

            switch(WhatsShowing)
            {
                case "Datasets":
                    bShowDatasests = true;
                    bScatteringApps = false;
                    bShowSystemData = false;
                    bShowSystemData = false;
                    bScatteringGPUs = false;
                    lbScatUsage.Text = "If more than one data set listed then clicking\non that name will hide / unhide it\nclick header to reset";
                    break;
                case "Apps" :
                    bScatteringApps = true;
                    bShowSystemData = false;
                    bShowDatasests = false;
                    bScatteringGPUs = false;
                    break;
                case "Systems":
                    bShowSystemData = true;
                    bShowDatasests = false;
                    bScatteringApps = false;
                    bScatteringGPUs = false;
                    break;
                case "GPUs":
                    bShowDatasests = false;
                    bScatteringApps = false;
                    bShowSystemData = false;
                    bScatteringGPUs = true;
                    cbUseOffset.Enabled = refSD.Count > 1;
                    lbOffsetValue.Visible = true;
                    lbScatUsage.Text = "If more than one data set listed then clicking\non that name will hide / unhide it\nclick header to reset";
                    break;
            }
            lviewSubSeries.Visible = bShowDatasests | bScatteringApps | bScatteringGPUs;
            btnInvSel.Visible = bShowDatasests && bSeeError; // does not work with any other scatter plots!!! 6-24-2019!!!
            // 2-3-2020 may want to look at this later
            //lblSysHideUnhide.Visible = lviewSubSeries.Visible;
            ThisSeriesData = refSD;
            ShowScatter();
            GetLegendInfo.Enabled=true;
            lviewSubSeries.ColumnClick += new ColumnClickEventHandler(ColumnClick); // did not see this in properties
        }



        class cColoredLegends
        {  
            public string strName;
            public string strSubItems;  // for now, we are only showing projects
            public Color rgb;
        }

 
        // show average value for each gpu be sure to correct for scaleing
        private string strFmtOffset(double dvalue)
        {
            if (dvalue == 0.0) return "";
            return (dvalue * fScaleMultiplier).ToString("#0.0");
        }

        List<cColoredLegends> MyLegendNames;
        List<DataPoint> SavedColoredPoints;
        private void FillInSeriesLegends()
        {
            int n = ThisSeriesData.Count;
            MyLegendNames = new List<cColoredLegends>();
            cColoredLegends cl = new cColoredLegends();
            SavedColoredPoints = new List<DataPoint>();
            ChartScatter.ApplyPaletteColors();
            cl.strName = "All Series";
            cl.rgb = Color.Black;
            MyLegendNames.Add(cl);
            foreach(cSeriesData sd in ThisSeriesData)
            {
                string seriesname = bScatteringGPUs ? sd.strSeriesName : sd.GetNameToShow(sd.ShowType);
                cl = new cColoredLegends();
                cl.strName = seriesname + "[avg:" + strFmtOffset(sd.dAvgs) + "]" ;
                cl.rgb = ChartScatter.Series[seriesname].Color;
                MyLegendNames.Add(cl);
            }
            for(int i = 0; i < ThisSeriesData.Count; i++)
            {
                if (ChartScatter.Series[i].Points.Count == 0)
                {
                    continue;
                }
                DataPoint p = new DataPoint();
                p.Color = ChartScatter.Series[i].Points[0].Color;
                SavedColoredPoints.Add(p);  // want to restore this color
            }
            // allow 1 more than actual so we can wrap back to 0
            nudShowOnly.Maximum = n + 1;
            // must have at least 2 series and "All" is not a series
            cbUseOffset.Enabled = (MyLegendNames.Count > 2) & bScatteringGPUs;
            DrawShowingText(0);
        }

        // how many valid points in series
        private int CountOnlyValids(int iSeries)
        {
            int n = 0;
            var stuff = ThisSeriesData[iSeries];
            {
               foreach(bool b in stuff.bIsValid)
                {
                    if(b)n++;
                }
            }
            return n;
        }

        private int CountWhatsShowing()
        {
            int n = 0;
            if(CurrentSeriesDisplayed >=0)
            {
                return CountOnlyValids(CurrentSeriesDisplayed);
            }
            for(int i = 0; i < CurrentNumberSeriesDisplayable;i++)
            {
                n += CountOnlyValids(i);
            }
            return n;
        }

        private void DrawShowingText(int i)
        {
            int iCountWhatsShowing = CountWhatsShowing();
            tboxShowing.Text = MyLegendNames[i].strName + " (" + iCountWhatsShowing.ToString() + ")";
            tboxShowing.ForeColor = MyLegendNames[i].rgb;
        }

        private double GetBestScaleingBottom(double a)
        {
            if (a < 100) return 0;
            if (a < 1000) return 100;
            return 1000;
        }

        // this is not as good as the next one lets see
        /*
        private double xGetBestScaleingUpper(double a)
        {
            int iSig = (int) nudXscale.Value;
            double r = 1.0 / (1 + iSig);
            if (a < 10) return Math.Max(a, 10 * r);
            if (a < 100) return Math.Max(a, 100 * r);
            if (a < 1000) return Math.Max(a, 1000 * r);
            return a;
        }
        */
        private double GetBestScaleingUpper(double a)
        {
            return GetBestOffsetScale(a);
        }

        // use (dOut / d) to scale
        // the input to this must be in minutes
        private string BestTimeUnits(double d, ref double dOut)
        {
            string strOut = " mins";
            dOut = d;
            if (d < 60.0) return strOut;
            strOut = " hours";
            dOut /= 60.0;
            if (dOut < 24) return  strOut;
            dOut /= 24.0;
            return " days";
        }

        private double cvtScale2Double(string str)
        {
            if (str == " mins") return 1.0;
            if (str == " hours") return 60.0;
            return 60*24;
        }

        // this from idea at stackoverflow
        private void HidePoint(DataPoint p)
        {
            p.IsEmpty = true;
            p.Color = Color.Transparent;    // was color.empty            
        }
        private void UnHidePoint(DataPoint p, Color c)
        {
            p.IsEmpty = false;
            p.Color = c;
        }

        private bool CalcMinMax(int iSeries)
        {
            double dSmall = 1e6;
            double dBig = -1;
            bool bValid = false;
            cSeriesData sd = ThisSeriesData[iSeries];
            int n = sd.dValues.Count;
            double d;
            for(int i = 0; i < n; i++)
            {
                if (!(sd.bIsValid[i] || bSeeError)) continue;
                d = sd.dValues[i];
                dSmall = Math.Min(dSmall, d);
                dBig = Math.Max(dBig, d);
                bValid = true;
            }
            sd.dSmall = dSmall;
            sd.dBig = dBig;
            return bValid;
        }

        /*
         * if elapsed time is over 60 seconds, scale time to minutes
         * same for minutes and hours
        */

        private string SetMinMax(ref double f)
        {
            double d = 0;
            int n = ThisSeriesData.Count;
            dSmall = 1e6;
            dBig = -1;           
            bool bValid;
            cSeriesData sd;
            for (int i = 0; i < n;i++)
            {
                sd = ThisSeriesData[i];
                bValid = CalcMinMax(i);
                if (sd.dSmall < dSmall) dSmall = sd.dSmall;
                if (sd.dBig > dBig) dBig = sd.dBig;               
            }
            string strUnits = BestTimeUnits(dBig, ref d);
            f = d / dBig;
            dSmall *= f;
            dBig = d;
            
            return strUnits;
        }

        private void SetScale()
        {
            fScaleMultiplier = 0;
            strScaleUnits = SetMinMax(ref fScaleMultiplier);
            dScaledOffset = GetBestOffsetScale(dOrigOffset * fScaleMultiplier);
        }


        // draw points vertical one over the other from 1 to n but normalzed to 0.0 to 1.0
        // the x axis position is the elapsed time.
        // only called once so cannnot use cbUseOffset tool
        private void ShowScatter()
        {
            double d = 0.0, e = 0.0;
            int j = 0;
            nWUtotal = 0;
            tbWUcnt.Text = "";
            //double dOffset = cbUseOffset.Checked ? dScaledOffset : 0.0 ; not useful here
            WUsPerSelection.Clear();
            CurrentNumberSeriesDisplayable = ThisSeriesData.Count;
            SetScale();
            //bScatteringApps = ThisSeriesData[0].bIsShowingApp;
            // lviewSubSeries.Visible = bScatteringApps;

            if (bShowSystemData)
            {
                lblShowApp.Text = "You are viewing: " + ThisSeriesData[0].strAppName;
            }
            else lblShowApp.Visible = false;
            foreach (cSeriesData sd in ThisSeriesData)
            {
                int n = sd.dValues.Count;
                WUsPerSelection.Add(n);
                nWUtotal += n;
                List<double> yAxis = new List<double>();
                List<double> xAxis = new List<double>();
                for (int i = 0; i < n; i++)
                {
                    d = Convert.ToDouble(i) / n;
                    yAxis.Add(d);
                    e = fScaleMultiplier * sd.dValues[i];
                    xAxis.Add(e);
                    //DataPoint pt = new DataPoint(e, d);
                }
                string seriesname = bScatteringGPUs ? sd.strSeriesName : sd.GetNameToShow(sd.ShowType);
                //seriesname += " WUs:" + n.ToString(); // cannot rename a series!
                tbWUcnt.Text += seriesname + " WUs:" + n.ToString() + Environment.NewLine;
                SeriesName = seriesname;
                ChartScatter.Series.Add(seriesname);
                ChartScatter.Series[seriesname].EmptyPointStyle.Color = Color.Transparent;
                // seems not needed but left in to remind of what I tried
                ChartScatter.Series[seriesname].ChartType = SeriesChartType.Point;
                ChartScatter.Series[seriesname].Points.DataBindXY(xAxis.ToArray(), yAxis.ToArray());
                ChartScatter.Series[seriesname].MarkerStyle = GetMS(j);
                n = 0;
                foreach(DataPoint p in ChartScatter.Series[seriesname].Points)
                {
                    if(n == 0)
                    {
                        int jj = 0;
                    }
                    p.Tag = bScatteringGPUs ? j :0 ; // was null ?sd.iGpuDevice[n];  
                    n++;
                }
                j++;
            }
            UsedOutliers = new Stack<cSaveOutlier>();
            ChartScatter.Legends["Legend1"].Title = strSeries + strScaleUnits;

            ChartScatter.ChartAreas["ChartArea1"].AxisX.Maximum = GetBestScaleingUpper(dBig);
            ChartScatter.ChartAreas["ChartArea1"].AxisX.Minimum = GetBestScaleingBottom(dSmall);
            ChartScatter.ChartAreas["ChartArea1"].AxisX.LabelStyle.Format = "#.#";
            LBtLwuS.Text = "Work units in above scatter plot:" + nWUtotal.ToString();
        }

        private void AddOffset()
        {
            if (dScaledOffset == 0.0) return;   // nothing to do
            double dOffset;
            for (int i = 0; i < ThisSeriesData.Count; i++)
            {
                dOffset = i * dScaledOffset;
                foreach (DataPoint p in ChartScatter.Series[i].Points)
                {
                    p.XValue += dOffset; 
                }
            }
            lbOffsetValue.Text = "each additional series offset by " + dScaledOffset.ToString("#0.0");
        }
        private void SubOffset()
        {
            if (dScaledOffset == 0.0) return;   // nothing to do
            double dOffset;
            for (int i = 0; i < ThisSeriesData.Count; i++)
            {
                dOffset = i * dScaledOffset;
                foreach (DataPoint p in ChartScatter.Series[i].Points)
                {
                    p.XValue -= dOffset;
                }
            }
            lbOffsetValue.Text = "no offset applied";
        }

        // we removed an outlier got to rescale
        private void DoRescaleXaxis(double f)
        {
            if (Math.Abs(f - 1.0) < .01) return;    // within rounding
            for(int i = 0; i < ThisSeriesData.Count;i++)
            {
                foreach(DataPoint p in ChartScatter.Series[i].Points)
                {
                    p.XValue *= f;
                }
            }
        }

        // setup the expected colors for systems for the selected app
        // expected offset is the ID of the system that was combined into the single series for the app being displayed
        // this ID is used to color just those systems to differenciate them in the plot WHEN JUST 1 APP IS DISPLAYED
        List<Color> MyColors = new List<Color>();
        List<int> ExpectedOffset = new List<int>();
        List<bool> SystemsDisplayed = new List<bool>();
        private void SetSysColors(int s)
        {
            int n = ThisSeriesData[s].TheseSystems.Count;
            rnd = new Random();
            MyColors.Clear();
            ExpectedOffset.Clear();
            for (int i = 0; i < n; i++)
            {
                ListViewItem itm = new ListViewItem();
                itm.Text = ThisSeriesData[s].TheseSystems[i];
                itm.ForeColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                itm.Tag = s;
                lviewSubSeries.Items.Add(itm);
                MyColors.Add(itm.ForeColor);
                ExpectedOffset.Add(ThisSeriesData[s].iTheseSystem[i]);
                SystemsDisplayed.Add(true); // all are drawn at first
            }
        }

        // for the series index s put the names of the systems into the list box
        // and arrange to show the different colors (if any)
        // not necessary for scatterning projects, only if scattering data
        private void ShowSystemNames(int s)
        {
            lviewSubSeries.Items.Clear();
            bool bAny = false;
            int n = ThisSeriesData[s].TheseSystems.Count;

            if (bShowDatasests)
            {
                // no need for colors but need stuff that is in that routine;
                ExpectedOffset.Clear();
                MyColors.Clear();
                for (int i = 0; i < n; i++)
                {
                    ListViewItem itm = new ListViewItem();
                    itm.Text = ThisSeriesData[s].TheseSystems[i];
                    itm.Tag = s;
                    lviewSubSeries.Items.Add(itm);
                    ExpectedOffset.Add(ThisSeriesData[s].iTheseSystem[i]);
                    SystemsDisplayed.Add(true); // all are drawn at first
                    MyColors.Add(SavedColoredPoints[s].Color);
                }
                return;
            }
            SetSysColors(s);
            n = ExpectedOffset.Count;
            if(n == 1 || bScatteringGPUs)
            {
                // no need to get random color, reuse the default
                return;
            }
            for (int i = 0; i < ThisSeriesData[s].dValues.Count; i++)
            {
                bAny = false;
                int x = ThisSeriesData[s].iSystem[i];   // index to system for this point
                DataPoint p = ChartScatter.Series[s].Points[i];
                for(int j = 0; j < n; j++)
                {
                    if( ExpectedOffset[j] == x)
                    {
                        p.Color = MyColors[j];
                        bAny = true;
                        break;
                    }
                }
                Debug.Assert(bAny);
            }
        }

        private void RestoreDefaultColors()
        {
            int n = ThisSeriesData.Count;
            if (bShowSystemData) return;
            for(int i = 0; i <n; i ++)
            {
                Color c = SavedColoredPoints[i].Color;
                foreach (DataPoint p in ChartScatter.Series[i].Points)
                {
                    if (p.IsEmpty) continue;
                    p.Color = c;
                   //p.IsEmpty = false;
                }
                ChartScatter.Series[i].Color = c;
            }
        }

        

        private void nudXscale_ValueChanged(object sender, EventArgs e)
        {
            ChartScatter.ChartAreas["ChartArea1"].AxisX.Maximum = GetBestScaleingUpper(dBig);
        }
 

        private void GetLegendInfo_Tick(object sender, EventArgs e)
        {
            GetLegendInfo.Enabled = false;
            //FindLegend();
            if(ThisSeriesData.Count == 1)
            {
                labelShowSeries.Visible = false;
                tboxShowing.Visible = false;
                nudShowOnly.Visible = false;
            }
            FillInSeriesLegends();
        }
    
        private void ShowHideSeries(int j)
        {
            int n = ThisSeriesData.Count;

            if (j == 0 )
            {
                CurrentSeriesDisplayed = -1;
                lbGPUvis.Visible = false;
                for (int i = 0; i < n; i++)
                {
                    ChartScatter.Series[i].MarkerStyle = GetMS(i);
                    foreach (DataPoint dp in ChartScatter.Series[i].Points)
                    {
                        dp.MarkerStyle = GetMS(i);
                    }
                    ChartScatter.Series[i].Enabled = true;
                }
                gboxOutlier.Visible = true;
            }
            else 
            {
                j--;
                for (int i = 0; i < n; i++)
                {
                    if (i == j)
                    {
                        CurrentSeriesDisplayed = i; // showing only this series of "ThisSeriesData[]"
                        lbGPUvis.Visible = true;
                        int iPnt = 0;
                        foreach (DataPoint dp in ChartScatter.Series[i].Points)
                        {
                            int iGPU = ThisSeriesData[j].iGpuDevice[iPnt];
                            dp.MarkerStyle = GetMS(iGPU); ;
                            iPnt++;
                        }
                        ChartScatter.Series[i].Enabled = true;
                    }
                    else
                        ChartScatter.Series[i].Enabled = false;
                }
                gboxOutlier.Visible = false;
            }
        }

        // show or hide each series individually or all
        //
        private void nudShowOnly_ValueChanged(object sender, EventArgs e)
        {
            int i = Convert.ToInt32(nudShowOnly.Value);
            int j = MyLegendNames.Count;
            if (j <= 1) return;
            if(i < 0)
            {
                i = j - 1;
                nudShowOnly.Value = i;
            }
            if (i == j)
            {
                i = 0;  // wrap back to 0
                //RestoreDefaultColors();
                // the above works here but is not needed
                nudShowOnly.Value = 0;
                lviewSubSeries.Items.Clear();
            }
            cbUseOffset.Enabled = (nudShowOnly.Value == 0) & bScatteringGPUs;   // do not add offset when viewing individual series
            ShowHideSeries(i);  // show or hide entire series (visibility only)
            DrawShowingText(i); // ShowHide must be done first
            if (i == 0  | bShowSystemData)
            {
                RestoreDefaultColors();
                lviewSubSeries.Items.Clear();
                if(i == 0)
                    LBtLwuS.Text = "Work units in above scatter plot:" + nWUtotal.ToString();
                else
                    LBtLwuS.Text = "Work units in above scatter plot:" + WUsPerSelection[i - 1].ToString();
                return; // not showing individual series nor scattering apps
            }
            LBtLwuS.Text = "Work units in above scatter plot:" + WUsPerSelection[i-1] .ToString();
            ShowSystemNames(i-1);       // in addition to names, points are shown
        }

        private void nudXscale_ValueChanged_1(object sender, EventArgs e)
        {
            ChartScatter.ChartAreas["ChartArea1"].AxisX.Maximum = GetBestScaleingUpper(dBig);
        }

        private void cboxUseLog_CheckedChanged(object sender, EventArgs e)
        {
            double d = GetBestScaleingBottom(dSmall);
            ChartScatter.ChartAreas["ChartArea1"].AxisX.Minimum = cboxUseLog.Checked ? Math.Max(0.01, d) : d ;
            ChartScatter.ChartAreas["ChartArea1"].AxisX.MinorTickMark.Enabled  = true;
            ChartScatter.ChartAreas["ChartArea1"].AxisX.MinorTickMark.Interval = 1;
            ChartScatter.ChartAreas["ChartArea1"].AxisX.IsLogarithmic = cboxUseLog.Checked;
            if(!cboxUseLog.Checked)
            {
                ChartScatter.ChartAreas["ChartArea1"].AxisX.Maximum = GetBestScaleingUpper(dBig);
            }
        }

        // data is not sorted so we will just traverse and get biggest
        private bool bGetLastOutlier(int iSeries, ref int iLoc, ref double xValue, ref Color OriginalColor)
        {
            double dBig = -1;
            int iWhereBig = -1;
            bool bAny = false;
            DataPoint p;
            int j = (CurrentSeriesDisplayed == -1) ? 0 : CurrentSeriesDisplayed;
            Color c = SavedColoredPoints[j].Color;  // just need something here
            int n = ChartScatter.Series[iSeries].Points.Count;
            for(int i = 0; i < n; i++)
            {
                p = ChartScatter.Series[iSeries].Points[i];
                if (p.IsEmpty) continue;
                if(p.XValue > dBig)
                {
                    dBig = p.XValue;
                    iWhereBig = i;
                    c = p.Color;
                    bAny = true;
                }
            }
            xValue = dBig;
            iLoc = iWhereBig;
            OriginalColor = c;
            return bAny;
        }

        private double FindOutlier(ref cSaveOutlier sO, ref int iWhereSeries)
        {
            double xValue = -1.0, dBig = -1;
            int iWherePoint = -1;
            int iLoc = -1;
            bool bAny = false;
            if(CurrentSeriesDisplayed >= 0)
            {
                bAny = bGetLastOutlier(CurrentSeriesDisplayed, ref iLoc, ref xValue, ref sO.c);
                return xValue;
            }
            for (int i = 0; i < CurrentNumberSeriesDisplayable; i++)
            {
                bAny = bGetLastOutlier(i, ref iLoc, ref xValue, ref sO.c);
                if (!bAny)
                {
                    continue;
                }
                if (dBig < xValue)
                {
                    dBig = xValue;
                    iWherePoint = iLoc;
                    iWhereSeries = i;
                }
            }
            if (iWhereSeries < 0 || iWherePoint < 0) return 0;    // all removed
            sO.seriesname = ChartScatter.Series[iWhereSeries].Name;
            sO.iWhereRemoved = iWherePoint; 
            return dBig;
        }

        // remove the outlier then rescale to next largest
        // outlier value from chart series is scaled, may not be in minutes
        private void RemoveOutlier()
        {
            double x1Value,x2Value, x2original;   // original means from actual data,not the graphed point values
            double dNewMax=0; // if we remove a maximum there is a new one unless series is empty
            int iNewMax = 0;  // where it is
            bool bAny;      // if no new max
            double fCurrentScale;   // multiplier that was last used
            int iWhereSeries = -1;
            int iWhereData = -1;    // where int the original data 
            cSaveOutlier sO = new cSaveOutlier();
            cSaveOutlier sO2;
            x1Value = FindOutlier(ref sO, ref iWhereSeries );
            if (iWhereSeries == -1) return; // nothing to remove
            HidePoint(ChartScatter.Series[iWhereSeries].Points[sO.iWhereRemoved]);

            if (CurrentSeriesDisplayed < 0)
                iWhereData = iWhereSeries;  // they are one and the same
            else iWhereData = CurrentSeriesDisplayed;
            sO.iWhereData = iWhereData;
            sO.fmpy = 1.0;  // this changes
            ThisSeriesData[iWhereData].bIsValid[sO.iWhereRemoved] = false;
            UsedOutliers.Push(sO);
            sO2 = new cSaveOutlier();
            // need to recalc the maximum to be consistent
            bAny = bGetLastOutlier(iWhereSeries, ref iNewMax, ref dNewMax, ref sO2.c);
            if(bAny)
            {
                ThisSeriesData[iWhereData].dBig = dNewMax;
                if (dNewMax > dBig)
                    dBig = dNewMax; // this will change with scaling change
            }
            iWhereSeries = -1;
            x2Value = FindOutlier(ref sO2, ref iWhereSeries);
            if (iWhereSeries < 0) return;
            fCurrentScale = cvtScale2Double(strScaleUnits);
            //SetScale();
            x2original = ThisSeriesData[iWhereSeries].dValues[sO2.iWhereRemoved];
            if(x2Value != x2original)
            {
                double fRescale = x2original / x2Value;
                double OriginalValueThere = x1Value * fCurrentScale;
                ChangeScale(x2original, fCurrentScale, ref sO.fmpy);
            }
        }

        private void nudHideXoutliers_ValueChanged(object sender, EventArgs e)
        {
            int n = (int) nudHideXoutliers.Value;
            double x1;
            if(UsedOutliers.Count < n)
            {
                RemoveOutlier();
            }
            else
            {
                cSaveOutlier sO = UsedOutliers.Pop();
                //ChartScatter.Series[sO.seriesname].Points[sO.iWhereRemoved].IsEmpty = false;
                UnHidePoint(ChartScatter.Series[sO.seriesname].Points[sO.iWhereRemoved],sO.c);
                x1 = ThisSeriesData[sO.iWhereData].dValues[sO.iWhereRemoved];
                ThisSeriesData[sO.iWhereData].bIsValid[sO.iWhereRemoved] = true;
                // x1 was biggest at the time it was removed
                Debug.Assert(x1 >= ThisSeriesData[sO.iWhereData].dBig);   // not true if offsetting gpus else true if same series
                ThisSeriesData[sO.iWhereData].dBig = x1;
                RestoreScale(x1, sO.fmpy);
            }
        }

        // xValue here is original data not from graph
        private void RestoreScale(double xValue, double fMpy)
        {
            double a,f,d = 0;
            strScaleUnits = BestTimeUnits(xValue, ref d);
            f = d / xValue;
            a = 1.0 / fMpy;
            DoRescaleXaxis(a);   

            dSmall *= f;
            dBig = GetBestScaleingUpper(d);
            ChartScatter.ChartAreas["ChartArea1"].AxisX.Maximum = dBig;

            ChartScatter.Legends["Legend1"].Title = strSeries + strScaleUnits;
        }

        // fMpy is current scale applied to original data
        // xAxis points unlikely in minutes
        private void ChangeScale(double xValue, double fMpy, ref double aUsed)
        {
            double a, f, d = 0;
            strScaleUnits = BestTimeUnits(xValue, ref d);
            f = d / xValue;
            a = fMpy * f;
            DoRescaleXaxis(a);       // chart data was scaled already

            dSmall *= a;
            dBig = GetBestScaleingUpper(d);
            ChartScatter.ChartAreas["ChartArea1"].AxisX.Maximum = dBig;
            ChartScatter.Legends["Legend1"].Title = strSeries + strScaleUnits;
            aUsed = a;
        }

        // hide or show the selected system
        // if iShowMe is 0 then "all" was selected
        private void TargetSystem(int iShowMe)
        {
            int iRawData = (int) lviewSubSeries.Items[iShowMe].Tag;
            int iSeries = CurrentSeriesDisplayed;
            int i = 0;
            int k = 0;
            int j;

            j = ExpectedOffset[iShowMe];
            List<int> iSystem = ThisSeriesData[iRawData].iSystem;
            foreach (DataPoint p in ChartScatter.Series[iSeries].Points)
            {
                if (p.IsEmpty) continue;
                if(iSystem[i] == j)
                {
                    p.Color = SystemsDisplayed[iShowMe] ?  Color.Transparent : MyColors[iShowMe];  
                }
                i++;
            }
            SystemsDisplayed[iShowMe] = !SystemsDisplayed[iShowMe];
        }

        private void ColumnClick(object sender, EventArgs e)
        {
            if (lviewSubSeries == null) return;
            if (lviewSubSeries.Items.Count == 0) return;
            int iRawData = (int)lviewSubSeries.Items[0].Tag;  // all same tag so does not matter which one
            int iSeries = CurrentSeriesDisplayed;
            int i = 0;
            int k = 0;
            List<int> iSystem = ThisSeriesData[iRawData].iSystem;
            foreach (DataPoint p in ChartScatter.Series[iSeries].Points)
            {
                if (p.IsEmpty) continue;
                k = iSystem[i];
                for(int j = 0; j < ExpectedOffset.Count;j++)
                {
                    if(k == ExpectedOffset[j])
                    {
                        p.Color = MyColors[j];
                        break;
                    }
                }

                i++;
            }
            for ( i = 0; i < SystemsDisplayed.Count; i++)
                SystemsDisplayed[i] = true;
        }

        //hide or show the selected system
        private void lviewSubSeries_SelectedIndexChanged(object sender, EventArgs e)
        {
            // may want to show only certain systems and this applies only to scattering systems
            if (bShowSystemData) return;   // does not apply here as data is homogenous
            if (lviewSubSeries.Items.Count == 0) return;
            ListView.SelectedIndexCollection indices = lviewSubSeries.SelectedIndices;
            if (indices.Count == 0) return;
            int iShowMe = indices[0];
            if (lviewSubSeries.Items.Count > 1) // if only 1 system, nothing to differenciate between 
            {
                TargetSystem(iShowMe);
                lviewSubSeries.Items[iShowMe].Selected = false;
            }
        }

        private void InvertSelections()
        {
            if (lviewSubSeries.Items.Count == 0) return;
            int iRawData = (int)lviewSubSeries.Items[0].Tag;
            int iSeries = CurrentSeriesDisplayed;
            List<int> iSystem = ThisSeriesData[iRawData].iSystem;

            for (int i = 0; i < SystemsDisplayed.Count; i++)
            {
                SystemsDisplayed[i] = !SystemsDisplayed[i];
            }
            for(int i = 0; i< iSystem.Count;i++)
            {
                int g = iSystem[i]; //group number at this point
                DataPoint p = ChartScatter.Series[iSeries].Points[i];
                if (p.IsEmpty) continue;
                if (SystemsDisplayed[g])
                {
                    p.Color = MyColors[g];  // all the same color
                }
                else
                {
                    p.Color = Color.Transparent;
                }
            }

        }

        private void btnInvSel_Click(object sender, EventArgs e)
        {
            InvertSelections();
        }

  
        private Int32? FindNearestPoint(ref DataPointCollection points, double x, double y, int iLoc)
        {
            string strTemp="";
            if (points == null) return null;
            if (points.Count == 0) return null;
            DataPoint point = new DataPoint();
            rnd = new Random();
            Func<DataPoint, double> getLength = (p) => Math.Sqrt(Math.Pow(p.XValue - x, 2) + Math.Pow(p.YValues[0] - y, 2));
            List<double> ClosePoint = new List<double>();
            foreach(DataPoint dp in points)
            {
                ClosePoint.Add(getLength(dp));
            }
            var sorted = ClosePoint 
                .Select((z,i) => new KeyValuePair<double,int>(z,i))
                .OrderBy(z => z.Key)
                .ToList();
            //List<double> B = sorted.Select(z => z.Key).ToList();
            List<int> idx = sorted.Select(z => z.Value).ToList();
            //double v = getLength(points[0]);
            int j = idx[0];
            int iGrp = ThisSeriesData[iLoc].iSystem[j];
            points[j].Color = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
            // problem: not showing any app names in list box if there is only one appname
            if (lviewSubSeries.Items.Count > 0) strTemp = lviewSubSeries.Items[iGrp].Text;
            else strTemp = ThisSeriesData[iLoc].strAppName;
            MessageBox.Show("member of: " + strTemp + "[D" + points[j].Tag +  "]" );
            return 0;
        }

        private void ChartScatter_MouseClick(object sender, MouseEventArgs e)
        {
            int n, UseMe = CurrentSeriesDisplayed;
            if (!bShowDatasests) return;
            if (CurrentSeriesDisplayed < 0)
            {
                // if -1 may still have only one series
                if (ThisSeriesData.Count > 1) return;
                UseMe = 0;
            }
            DataPointCollection Points =
                    ChartScatter.Series[UseMe].Points;
            n = Points.Count;
            if (n > 250) return;
            Point p = e.Location;
            // Int32? iLoc = FindNearestPoint(Points, p);
            double x = ChartScatter.ChartAreas[0].AxisX.PixelPositionToValue(e.X);
            double y = ChartScatter.ChartAreas[0].AxisY.PixelPositionToValue(e.Y);
            FindNearestPoint(ref Points, x, y, UseMe);
        }

        private void cbUseOffset_CheckedChanged(object sender, EventArgs e)
        {
            nudShowOnly.Enabled = !cbUseOffset.Checked;
            if (MyLegendNames.Count <= 1) return;   // nothing to offset if only one dataset
            if (cbUseOffset.Checked)
            {
                nudHideXoutliers.Enabled = false;
                AddOffset();
                ChartScatter.ChartAreas["ChartArea1"].AxisX.Maximum = GetBestOffsetScale(dBig * MyLegendNames.Count);
                return;
            }
            nudHideXoutliers.Enabled = true;
            SubOffset();
            ChartScatter.ChartAreas["ChartArea1"].AxisX.Maximum = GetBestScaleingUpper(dBig);
        }

        private double GetBestOffsetScale(double dValue)
        {
            int iVal;
            if (dValue < 10.0) return 10.0;
            if (dValue < 100.0)
            {
                iVal = Convert.ToInt32(dValue / 10.0);
                iVal++;
                iVal *= 10;
            }
            else if(dValue < 1000.0)
            {
                iVal = Convert.ToInt32(dValue / 100.0);
                iVal++;
                iVal *= 100;
            }
            else
            {
                iVal = Convert.ToInt32(dValue / 1000.0);
                iVal++;
                iVal *= 1000;
            }
            return Convert.ToDouble(iVal);
        }
    }
}