using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using JR.Utils.GUI.Forms;

namespace ContourUnions
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        public MainControl()
        {
            InitializeComponent();
          
        }

        //---------------------------------------------------------------------------------
        #region default constants
        
        // MAX STRING LENGTH DEFAULTS
        const int MAX_PREFIX_LENGTH = 7;
        const int MAX_ID_LENGTH = 15; // reduced from 16 to 15 to account for space bw prefix and id

        // AVOIDANCE STRUCTURE DEFAULTS
        const string DEFAULT_AVOIDANCE_PREFIX = "zHK";
        const int DEFAULT_AVOIDANCE_GROW_MARGIN = 2;
        const int DEFAULT_AVOIDANCE_PTV_Counter = 1;
        const int DEFAULT_AVOIDANCE_CROP_MARGIN = 2;
        const int DEFAULT_AVOID_CROP_FROM_BODY_MARGIN = 0;
        const string AVOIDANCE_DICOM_TYPE = "AVOIDANCE";

        // OPTI STRUCTURE DEFAULTS
        const string DEFAULT_OPTI_PREFIX = "";
        const int DEFAULT_OPTI_GROW_MARGIN = 1;
        const int DEFAULT_OPTI_CROP_MARGIN = 2;
        const int DEFAULT_OPTI_CROP_FROM_BODY_MARGIN = 4;
        const string OPTI_DICOM_TYPE = "PTV";

        // RING STRUCTURE DEFAULTS
        const string DEFAULT_RING_PREFIX = "zRing";
        const int DEFAULT_RING_GROW_MARGIN = 10;
        const int DEFAULT_RING_COUNT = 3;
        const int DEFAULT_RING_CROP_MARGIN = 0;
        const int DEFAULT_RING_Crop = 3;
        const string RING_DICOM_TYPE = "CONTROL";

        // MISC DICOM TYPE DEFAULTS
        const string CONTROL_DICOM_TYPE = "CONTROL";

        // CI STRUCTURE DEFAULTS
        const string DEFAULT_CI_PREFIX = "zCI";
        const int DEFAULT_CI_GROW_MARGIN = 5;

        // R50 STRUCTURE DEFAULTS
        const string DEFAULT_R50_PREFIX = "zR50";
        const int DEFAULT_R50_GROW_MARGIN = 30;

        #endregion default constants
        //---------------------------------------------------------------------------------
        #region public variables

        // PUBLIC VARIABLES
        public Window zwindow;
        public string script = "Contour-Unions";
        public Patient patient;
        public PlanSum psum;
        public StructureSet ss;
        public PlanSetup planSetup;
        public double rxSum;

        public IEnumerable<Structure> sorted_gtvList;
        public IEnumerable<Structure> sorted_ctvList;
        public IEnumerable<Structure> sorted_itvList;
        public IEnumerable<Structure> sorted_ptvList;
        public IEnumerable<Structure> sorted_targetList;
        public IEnumerable<Structure> sorted_oarList;
        public IEnumerable<Structure> sorted_structureList;
        public IEnumerable<Structure> sorted_emptyStructuresList;
        public IEnumerable<Structure> sorted_zptvList;
        
        public string user;
        public double dosePerFraction;
        public string day;
        public string month;
        public string year;
        public string dayOfWeek;
        public string hour;
        public string minute;
        public string timeStamp;
        public string curredLastName;
        public string curredFirstName;
        public string firstInitial;
        public string lastInitial;
        public string initials;
        public string id;
        public string randomId;
        public string courseName;
        public string ssId;
        public bool isGrady;
        public bool cropFromBody = false;
        public int cropFromBodyMargin = DEFAULT_OPTI_CROP_FROM_BODY_MARGIN;
        public bool hasNoPTV = false;
        public bool hasSinglePTV = false;
        public bool hasMultiplePTVs = false;
        public bool hasTwoDoseLevels = false;
        public bool hasThreeDoseLevels = false;
        public bool hasFourDoseLevels = false;
        public string highresMessage;
        public bool hasHighRes = false;
        public bool hasHighResTargets = false;
        public bool needHRStructures = false;
        private string MESSAGES = string.Empty;
        private int counter = 0;
        public bool createCI = false;
        public bool createR50 = false;
        public double doseLevel1CropMargin = DEFAULT_OPTI_CROP_MARGIN;
        public double doseLevel2CropMargin = DEFAULT_OPTI_CROP_MARGIN;
        public double doseLevel3CropMargin = DEFAULT_OPTI_CROP_MARGIN;

        public bool boolAllTargetsForAvoids = false;
        public bool createAvoidsForMultipleTargets = false;
        public int avoidTarget1CropMargin = DEFAULT_AVOIDANCE_CROP_MARGIN;
        public int avoidTarget2CropMargin = DEFAULT_AVOIDANCE_CROP_MARGIN;
        public int avoidTarget3CropMargin = DEFAULT_AVOIDANCE_CROP_MARGIN;
        public int avoidTarget4CropMargin = DEFAULT_AVOIDANCE_CROP_MARGIN;

        public bool createPTVEval = false;

        #endregion public variables
        //---------------------------------------------------------------------------------
        #region objects used for binding

        #endregion
        //---------------------------------------------------------------------------------
        #region paths and string builders for data collection scripts

        #region Script Use Log

        public string userLogPath= Directory.Exists(@"\\Network-Path\")? @"\\\Network-Path\ContourUnion_UserLog.csv" : System.IO.Path.GetTempPath()+ "ContourUnions_UserLog.csv";


        public StringBuilder userLogCsvContent = new StringBuilder();

        #endregion

        #endregion
        //---------------------------------------------------------------------------------
        #region event controls

        #region button / checkbox events

        // check boxes

        // create structures button
        public void CreateStructures_Btn_Click(object sender, RoutedEventArgs e)
        {
            
            List<string> allCurrent_structures = new List<string>();

            foreach (Structure d in ss.Structures)
            {
                allCurrent_structures.Add(d.Id);
            }

            // will be changed to false if any validation logic fails
            var continueToCreateStructures = true;

            // determine further whether High Res Structures are needed
            foreach (var s in OarList_LV.SelectedItems)
            {
                if (highresMessage.Contains(s.ToString()))
                {
                    needHRStructures = true;
                }
            }

            foreach (var s in PTVList_LV.SelectedItems)
            {
                if (highresMessage.Contains(s.ToString()))
                {
                    needHRStructures = true;
                    hasHighResTargets = true;
                }
            }


        // NOTE: various other validation can be done. To reduce the need for some validation, when user input for margins is invalid, a warning will show informing the user of the invalid entry and that the default value will instead be used. 
        if (continueToCreateStructures)
        {
            using (new WaitCursor())
            {
                // DUBUG: samle message for debugging -- rest have been removed
                //MessageBox.Show(string.Format("{0}", tempCounter));
                //tempCounter += 1;

                // add messages description
                MESSAGES += string.Format("Some General Tasks/Issues during script run: {0}", counter+1);
                //if (needHRStructures) { MESSAGES += "\r\n\t- Some of the selected structures are High Res Structures so\r\n\t\ta High Res Body and High Res Opti Total will be created"; }

                #region variables

                // progress counter in case user clicks Create Structures Button more than once during same instance of script
                counter += 1;

                // for high res structures
                // create high res body for cropping
                Structure bodyHR = null;
                string bodyHRId = "zzBODY_HR";

                // create zopti total High Res for booleans/cropping
                //Structure zptvTotalHR = null;
                //Structure zoptiTotalHR = null;
                Structure zptvsSelected = null;
                Structure zptvsSelectedHR = null;

                Structure zzptvsSelected = null;
                Structure zzptvsSelectedHR = null;
                Structure zzctvsSelected = null;
                Structure zzctvsSelectedHR = null;
                Structure zzgtvsSelected = null;
                Structure zzgtvsSelectedHR = null;

                int endPTV = AvoidCount_TextBox.Text != "" ? int.Parse(AvoidCount_TextBox.Text) + sorted_ptvList.Count() - 1 : sorted_ptvList.Count();
                int startPTV = AvoidCount_TextBox.Text != "" ? int.Parse(AvoidCount_TextBox.Text) : 1;
                //string zptvTotalHRId = startPTV != endPTV ? "zPTV" + startPTV.ToString() + "-" + endPTV.ToString() + "_all_HR" : "zPTV" + startPTV.ToString() + "_all_HR";
                //string zoptiTotalHRId = "zopti total_HR";

                string zptvSelectedId = "";
                string zptvSelectedHRId ="";
                string zzptvSelectedId = "";
                string zzptvSelectedHRId = "";
                string zzctvSelectedId = "";
                string zzctvSelectedHRId = "";
                string zzgtvSelectedId = "";
                string zzgtvSelectedHRId = "";

                // for when optis are made
                int zoptiGrowMargin = 0;

                #endregion variables

                // allow modifications
                patient.BeginModifications();

                #region find body

                // find body
                Structure body = null;
                try
                {
                    body = Helpers.GetBody(ss);
                    //MessageBox.Show(string.Format("{0}", body.IsHighResolution));
                    if (body.HasSegment && body.IsEmpty)
                    {
                        //body = ss.CreateAndSearchBody(ss.GetDefaultSearchBodyParameters());

                        var message = "Sorry, the BODY or EXTERNAL Structure was empty:\n\n\t- A Body Structure will be generated using the default Search Body Parameters. Please verify accuracy.";
                        var title = "Body Structure Generation";

                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch
                {
                    var message = "Sorry, could not find a structure of type BODY:\n\n\t- A Body Structure will be generated using the default Search Body Parameters. Please verify accuracy.";
                    var title = "Body Structure Generation";

                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                #endregion find body

                #region get high res structures

                if (needHRStructures)
                {
                    // remove if already there
                    Helpers.RemoveStructure(ss, bodyHRId);
                    // Helpers.RemoveStructure(ss, zptvTotalHRId);
                    //Helpers.RemoveStructure(ss, zoptiTotalHRId);

                    // add empty structures
                    bodyHR = ss.AddStructure(CONTROL_DICOM_TYPE, bodyHRId);
                    // zptvTotalHR = ss.AddStructure(OPTI_DICOM_TYPE, zptvTotalHRId);
                    //zoptiTotalHR = ss.AddStructure(OPTI_DICOM_TYPE, zoptiTotalHRId);

                    // copy body to bodyHR
                    bodyHR.SegmentVolume = body.SegmentVolume;

                    // convert to high res
                    if (bodyHR.CanConvertToHighResolution() == true) { bodyHR.ConvertToHighResolution(); /*MESSAGES += "\r\n\t- High Res Body Created";*/ }
                    //if (zptvTotalHR.CanConvertToHighResolution() == true) { zptvTotalHR.ConvertToHighResolution(); /*MESSAGES += "\r\n\t- High Res Target Created";*/ }
                    //if (zoptiTotalHR.CanConvertToHighResolution() == true) { zoptiTotalHR.ConvertToHighResolution(); /*MESSAGES += "\r\n\t- High Res Target Created";*/ }

                       
                }

                #endregion get high res structures


                #region UnionHelper

                if (CreatePTVunions_CB.IsChecked == true)
                {
                    var optiStructuresToMake = PTVList_LV.SelectedItems;
                    List<Structure> selected_ptvList = new List<Structure>();

                    foreach (Structure d in sorted_ptvList)
                    {
                        foreach (var g in optiStructuresToMake)
                        {
                            if (d.Id == g.ToString())
                            {
                                selected_ptvList.Add(d);
                            }
                        }
                    }
                    bool needselectedptvHRStructure = false;
                    foreach (var t in selected_ptvList)
                    {
                        if (t.IsHighResolution) { needselectedptvHRStructure = true; }
                    }


                    int end2PTV = PTVCount_TextBox.Text != "" ? int.Parse(PTVCount_TextBox.Text) + selected_ptvList.Count() - 1 : selected_ptvList.Count();
                    int start2PTV = PTVCount_TextBox.Text != "" ? int.Parse(PTVCount_TextBox.Text) : 1;

                    zzptvSelectedHRId = start2PTV != end2PTV ? "zPTV" + start2PTV.ToString() + "-" + end2PTV.ToString() + "_HRsele" : "zPTV" + start2PTV.ToString() + "_HRsele";

                    if (ptvUnionID_TextBox.Text.Trim() == "")
                    {
                        zzptvSelectedId = start2PTV != end2PTV ? "zPTV" + start2PTV.ToString() + "-" + end2PTV.ToString() + "_select" : "zPTV" + start2PTV.ToString() + "_select";
                    }
                    else if (ptvUnionID_TextBox.Text.Length >16)
                    {
                        zzptvSelectedId = ptvUnionID_TextBox.Text.Substring(0, 16).Trim().Replace(",", "");
                    }
                    else
                    {
                        zzptvSelectedId = ptvUnionID_TextBox.Text.Trim().Replace(",", "");
                    }

                    // remove if already there
                    try
                    {
                        Helpers.RemoveStructure(ss, zzptvSelectedId);
                    }
                    catch
                    {
                        throw new ApplicationException(string.Format("The structure '{0}' already exists and is approved. Please unapprove or influence the new ID.\n\nThe script has to terminate.", zzptvSelectedId));
                    }

                    // add empty ptv selected structure
                    zzptvsSelected = ss.AddStructure("PTV", zzptvSelectedId);

                    if (needselectedptvHRStructure == false)
                    {
                        // boolean ptvs into zopti total
                        zzptvsSelected.SegmentVolume = selected_ptvList.Count() > 1 ? Helpers.BooleanStructures(ss, selected_ptvList) : selected_ptvList.First().SegmentVolume;
                        MESSAGES += string.Format("\r\n\t-  Structure Booleaned: {0}", zzptvSelectedId);
                            

                        if (applyMarginPTV_CB.IsChecked == true & !PTVmargin_TextBox.Text.Equals("0") & !PTVmargin_TextBox.Text.Equals(""))
                        {
                            double ptvUnionMargin;

                            // set grow margin
                            if (PTVmargin_TextBox.Text == "" || string.IsNullOrWhiteSpace(PTVmargin_TextBox.Text)) { ptvUnionMargin = 0; }
                            else
                            {
                                if (double.TryParse(PTVmargin_TextBox.Text, out ptvUnionMargin))
                                {
                                    //parsing successful 
                                }
                                else
                                {
                                    //parsing failed. 
                                    ptvUnionMargin = 0;
                                    MessageBox.Show(string.Format("Oops, an invalid value was used for the avoidance structure grow margin ({0}). The DEFAULT of {1} mm will be used.", PTVmargin_TextBox.Text, 0));
                                }
                            }
                            zzptvsSelected.SegmentVolume = zzptvsSelected.Margin(ptvUnionMargin);
                            MESSAGES += string.Format("\r\n\t-  Margin of {1}mm applied to: {0}", zzptvSelectedId, PTVmargin_TextBox.Text);
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzptvSelectedId, zzptvsSelected.GetNumberOfSeparateParts());
                        }
                        else
                        {
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzptvSelectedId, zzptvsSelected.GetNumberOfSeparateParts());
                        }
                    }
                    else
                    {
                        // remove if already there
                        try
                        {
                            Helpers.RemoveStructure(ss, zzptvSelectedHRId);
                        }
                        catch
                        {
                            throw new ApplicationException(string.Format("The structure '{0}' already exists and is approved. Please unapprove or influence the new ID.\n\nThe script has to terminate.", zzptvSelectedHRId));
                        }
                        zzptvsSelectedHR = ss.AddStructure("PTV", zzptvSelectedHRId);
                        zzptvsSelectedHR.ConvertToHighResolution();
                        zzptvsSelected.ConvertToHighResolution();
                        foreach (var t in selected_ptvList)
                        {
                            Structure hrTarget = null;
                            var hrId = string.Format("zz{0}_HR", Helpers.ProcessStructureId(t.Id.ToString().Replace(" ", ""), MAX_ID_LENGTH - 5));
                            // remove if already there
                            Helpers.RemoveStructure(ss, hrId);

                            // add empty zopti total structure
                            hrTarget = ss.AddStructure(OPTI_DICOM_TYPE, hrId);
                            hrTarget.SegmentVolume = t.SegmentVolume;
                            if (hrTarget.CanConvertToHighResolution()) { hrTarget.ConvertToHighResolution(); } // TODO: may need to check if t is HR and then convert hrTarget to HR first?
                            zzptvsSelectedHR.SegmentVolume = zzptvsSelectedHR.Or(hrTarget.SegmentVolume);
                            Helpers.RemoveStructure(ss, hrId);
                        }
                        //zptvTotalHR.SegmentVolume = Helpers.CropOutsideBodyWithMargin(zptvTotalHR, bodyHR, 0);
                        zzptvsSelected.SegmentVolume = zzptvsSelectedHR.SegmentVolume;
                        Helpers.RemoveStructure(ss, zzptvSelectedHRId);
                        MESSAGES += string.Format("\r\n\t-  Structure Booleaned: {0}", zzptvSelectedId);
                        MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} 3D-parts", zzptvSelectedId, zzptvsSelected.GetNumberOfSeparateParts());
                        if (applyMarginPTV_CB.IsChecked == true & !PTVmargin_TextBox.Text.Equals("0") & !PTVmargin_TextBox.Text.Equals(""))
                        {
                            double ptvUnionMargin;
                            //Structure unionPTVplus = ss.AddStructure(PTV, zzptvSelectedId+"_");
                            // set grow margin
                            if (PTVmargin_TextBox.Text == "" || string.IsNullOrWhiteSpace(PTVmargin_TextBox.Text)) { ptvUnionMargin = DEFAULT_AVOIDANCE_GROW_MARGIN; }
                            else
                            {
                                if (double.TryParse(PTVmargin_TextBox.Text, out ptvUnionMargin))
                                {
                                    //parsing successful 
                                }
                                else
                                {
                                    //parsing failed. 
                                    ptvUnionMargin = 0;
                                    MessageBox.Show(string.Format("Oops, an invalid value was used for the avoidance structure grow margin ({0}). The DEFAULT of {1} mm will be used.", PTVmargin_TextBox.Text, 0));
                                }
                            }
                            zzptvsSelected.SegmentVolume = zzptvsSelected.Margin(ptvUnionMargin);
                            //Helpers.RemoveStructure(ss, s);
                            MESSAGES += string.Format("\r\n\t-  Margin of {1}mm applied to: {0}", zzptvSelectedId, PTVmargin_TextBox.Text);
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzptvSelectedId, zzptvsSelected.GetNumberOfSeparateParts());
                        }
                        else
                        {
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzptvSelectedId, zzptvsSelected.GetNumberOfSeparateParts());
                        }
                    }
                }

                if (CreateCTVUnions_CB.IsChecked == true)
                {
                    var optiStructuresToMake = CTVList_LV.SelectedItems;
                    List<Structure> selected_ctvList = new List<Structure>();

                    foreach (Structure d in sorted_ctvList)
                    {
                        foreach (var g in optiStructuresToMake)
                        {
                            if (d.Id == g.ToString())
                            {
                                selected_ctvList.Add(d);
                            }
                        }
                    }
                    bool needselectedctvHRStructure = false;
                    foreach (var t in selected_ctvList)
                    {
                        if (t.IsHighResolution) { needselectedctvHRStructure = true; }
                    }


                    int end2CTV = CTVCount_TextBox.Text != "" ? int.Parse(CTVCount_TextBox.Text) + selected_ctvList.Count() - 1 : selected_ctvList.Count();
                    int start2CTV = CTVCount_TextBox.Text != "" ? int.Parse(CTVCount_TextBox.Text) : 1;

                    zzctvSelectedHRId = start2CTV != end2CTV ? "zCTV" + start2CTV.ToString() + "-" + end2CTV.ToString() + "_HRsele" : "zCTV" + start2CTV.ToString() + "_HRsele";

                    if (ctvUnionID_TextBox.Text.Trim() == "")
                    {
                        zzctvSelectedId = start2CTV != end2CTV ? "zCTV" + start2CTV.ToString() + "-" + end2CTV.ToString() + "_select" : "zCTV" + start2CTV.ToString() + "_select";
                    }
                    else if (ctvUnionID_TextBox.Text.Length > 16)
                    {
                        zzctvSelectedId = ctvUnionID_TextBox.Text.Substring(0, 16).Trim().Replace(",", "");
                    }
                    else
                    {
                        zzctvSelectedId = ctvUnionID_TextBox.Text.Trim().Replace(",", "");
                    }

                    // remove if already there
                    try
                    {
                        Helpers.RemoveStructure(ss, zzctvSelectedId);
                    }
                    catch
                    {
                        throw new ApplicationException(string.Format("The structure '{0}' already exists and is approved. Please unapprove or influence the new ID.\n\nThe script has to terminate.", zzctvSelectedId));
                    }

                    // add empty ctv selected structure
                    zzctvsSelected = ss.AddStructure("CTV", zzctvSelectedId);

                    if (needselectedctvHRStructure == false)
                    {
                        // boolean ctvs into zopti total
                        zzctvsSelected.SegmentVolume = selected_ctvList.Count() > 1 ? Helpers.BooleanStructures(ss, selected_ctvList) : selected_ctvList.First().SegmentVolume;
                        MESSAGES += string.Format("\r\n\t-  Structure Booleaned: {0}", zzctvSelectedId);


                        if (applyMarginCTV_CB.IsChecked == true & !CTVmargin_TextBox.Text.Equals("0") & !CTVmargin_TextBox.Text.Equals(""))
                        {
                            double ctvUnionMargin;

                            // set grow margin
                            if (CTVmargin_TextBox.Text == "" || string.IsNullOrWhiteSpace(CTVmargin_TextBox.Text)) { ctvUnionMargin = 0; }
                            else
                            {
                                if (double.TryParse(CTVmargin_TextBox.Text, out ctvUnionMargin))
                                {
                                    //parsing successful 
                                }
                                else
                                {
                                    //parsing failed. 
                                    ctvUnionMargin = 0;
                                    MessageBox.Show(string.Format("Oops, an invalid value was used for the avoidance structure grow margin ({0}). The DEFAULT of {1} mm will be used.", CTVmargin_TextBox.Text, 0));
                                }
                            }
                            zzctvsSelected.SegmentVolume = zzctvsSelected.Margin(ctvUnionMargin);
                            MESSAGES += string.Format("\r\n\t-  Margin of {1}mm applied to: {0}", zzctvSelectedId, CTVmargin_TextBox.Text);
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzctvSelectedId, zzctvsSelected.GetNumberOfSeparateParts());
                        }
                        else
                        {
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzctvSelectedId, zzctvsSelected.GetNumberOfSeparateParts());
                        }
                    }
                    else
                    {
                        // remove if already there
                        try
                        {
                            Helpers.RemoveStructure(ss, zzctvSelectedHRId);
                        }
                        catch
                        {
                            throw new ApplicationException(string.Format("The structure '{0}' already exists and is approved. Please unapprove or influence the new ID.\n\nThe script has to terminate.", zzctvSelectedHRId));
                        }
                        zzctvsSelectedHR = ss.AddStructure("CTV", zzctvSelectedHRId);
                        zzctvsSelectedHR.ConvertToHighResolution();
                        zzctvsSelected.ConvertToHighResolution();
                        foreach (var t in selected_ctvList)
                        {
                            Structure hrTarget = null;
                            var hrId = string.Format("zz{0}_HR", Helpers.ProcessStructureId(t.Id.ToString().Replace(" ", ""), MAX_ID_LENGTH - 5));
                            // remove if already there
                            Helpers.RemoveStructure(ss, hrId);

                            // add empty zopti total structure
                            hrTarget = ss.AddStructure(OPTI_DICOM_TYPE, hrId);
                            hrTarget.SegmentVolume = t.SegmentVolume;
                            if (hrTarget.CanConvertToHighResolution()) { hrTarget.ConvertToHighResolution(); } // TODO: may need to check if t is HR and then convert hrTarget to HR first?
                            zzctvsSelectedHR.SegmentVolume = zzctvsSelectedHR.Or(hrTarget.SegmentVolume);
                            Helpers.RemoveStructure(ss, hrId);
                        }
                   
                        zzctvsSelected.SegmentVolume = zzctvsSelectedHR.SegmentVolume;
                        Helpers.RemoveStructure(ss, zzctvSelectedHRId);
                        MESSAGES += string.Format("\r\n\t-  Structure Booleaned: {0}", zzctvSelectedId);
                        MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} 3D-parts", zzctvSelectedId, zzctvsSelected.GetNumberOfSeparateParts());
                        if (applyMarginCTV_CB.IsChecked == true & !CTVmargin_TextBox.Text.Equals("0") & !CTVmargin_TextBox.Text.Equals(""))
                        {
                            double ctvUnionMargin;
                            //Structure unionCTVplus = ss.AddStructure(CTV, zzctvSelectedId+"_");
                            // set grow margin
                            if (CTVmargin_TextBox.Text == "" || string.IsNullOrWhiteSpace(CTVmargin_TextBox.Text)) { ctvUnionMargin = DEFAULT_AVOIDANCE_GROW_MARGIN; }
                            else
                            {
                                if (double.TryParse(CTVmargin_TextBox.Text, out ctvUnionMargin))
                                {
                                    //parsing successful 
                                }
                                else
                                {
                                    //parsing failed. 
                                    ctvUnionMargin = 0;
                                    MessageBox.Show(string.Format("Oops, an invalid value was used for the avoidance structure grow margin ({0}). The DEFAULT of {1} mm will be used.", CTVmargin_TextBox.Text, 0));
                                }
                            }
                            zzctvsSelected.SegmentVolume = zzctvsSelected.Margin(ctvUnionMargin);
                            //Helpers.RemoveStructure(ss, s);
                            MESSAGES += string.Format("\r\n\t-  Margin of {1}mm applied to: {0}", zzctvSelectedId, CTVmargin_TextBox.Text);
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzctvSelectedId, zzctvsSelected.GetNumberOfSeparateParts());
                        }
                        else
                        {
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzctvSelectedId, zzctvsSelected.GetNumberOfSeparateParts());
                        }
                    }
                }

                if (CreateGTVunions_CB.IsChecked == true)
                {
                    var optiStructuresToMake = GTVList_LV.SelectedItems;
                    List<Structure> selected_gtvList = new List<Structure>();

                    foreach (Structure d in sorted_gtvList)
                    {
                        foreach (var g in optiStructuresToMake)
                        {
                            if (d.Id == g.ToString())
                            {
                                selected_gtvList.Add(d);
                            }
                        }
                    }
                    bool needselectedgtvHRStructure = false;
                    foreach (var t in selected_gtvList)
                    {
                        if (t.IsHighResolution) { needselectedgtvHRStructure = true; }
                    }


                    int end2GTV = GTVCount_TextBox.Text != "" ? int.Parse(GTVCount_TextBox.Text) + selected_gtvList.Count() - 1 : selected_gtvList.Count();
                    int start2GTV = GTVCount_TextBox.Text != "" ? int.Parse(GTVCount_TextBox.Text) : 1;

                    zzgtvSelectedHRId = start2GTV != end2GTV ? "zGTV" + start2GTV.ToString() + "-" + end2GTV.ToString() + "_HRsele" : "zGTV" + start2GTV.ToString() + "_HRsele";

                    if (gtvUnionID_TextBox.Text.Trim() == "")
                    {
                        zzgtvSelectedId = start2GTV != end2GTV ? "zGTV" + start2GTV.ToString() + "-" + end2GTV.ToString() + "_select" : "zGTV" + start2GTV.ToString() + "_select";
                    }
                    else if (gtvUnionID_TextBox.Text.Length > 16)
                    {
                        zzgtvSelectedId = gtvUnionID_TextBox.Text.Substring(0, 16).Trim().Replace(",", "");
                    }
                    else
                    {
                        zzgtvSelectedId = gtvUnionID_TextBox.Text.Trim().Replace(",", "");
                    }

                    // remove if already there
                    try
                    {
                        Helpers.RemoveStructure(ss, zzgtvSelectedId);
                    }
                    catch
                    {
                        throw new ApplicationException(string.Format("The structure '{0}' already exists and is approved. Please unapprove or influence the new ID.\n\nThe script has to terminate.", zzgtvSelectedId));
                    }

                    // add empty gtv selected structure
                    zzgtvsSelected = ss.AddStructure("GTV", zzgtvSelectedId);

                    if (needselectedgtvHRStructure == false)
                    {
                        // boolean gtvs into zopti total
                        zzgtvsSelected.SegmentVolume = selected_gtvList.Count() > 1 ? Helpers.BooleanStructures(ss, selected_gtvList) : selected_gtvList.First().SegmentVolume;
                        MESSAGES += string.Format("\r\n\t-  Structure Booleaned: {0}", zzgtvSelectedId);


                        if (applyMarginGTV_CB.IsChecked == true & !GTVmargin_TextBox.Text.Equals("0") & !GTVmargin_TextBox.Text.Equals(""))
                        {
                            double gtvUnionMargin;

                            // set grow margin
                            if (GTVmargin_TextBox.Text == "" || string.IsNullOrWhiteSpace(GTVmargin_TextBox.Text)) { gtvUnionMargin = 0; }
                            else
                            {
                                if (double.TryParse(GTVmargin_TextBox.Text, out gtvUnionMargin))
                                {
                                    //parsing successful 
                                }
                                else
                                {
                                    //parsing failed. 
                                    gtvUnionMargin = 0;
                                    MessageBox.Show(string.Format("Oops, an invalid value was used for the avoidance structure grow margin ({0}). The DEFAULT of {1} mm will be used.", GTVmargin_TextBox.Text, 0));
                                }
                            }
                            zzgtvsSelected.SegmentVolume = zzgtvsSelected.Margin(gtvUnionMargin);
                            MESSAGES += string.Format("\r\n\t-  Margin of {1}mm applied to: {0}", zzgtvSelectedId, GTVmargin_TextBox.Text);
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzgtvSelectedId, zzgtvsSelected.GetNumberOfSeparateParts());
                        }
                        else
                        {
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzgtvSelectedId, zzgtvsSelected.GetNumberOfSeparateParts());
                        }
                    }
                    else
                    {
                        // remove if already there
                        try
                        {
                            Helpers.RemoveStructure(ss, zzgtvSelectedHRId);
                        }
                        catch
                        {
                            throw new ApplicationException(string.Format("The structure '{0}' already exists and is approved. Please unapprove or influence the new ID.\n\nThe script has to terminate.", zzgtvSelectedHRId));
                        }
                        zzgtvsSelectedHR = ss.AddStructure("GTV", zzgtvSelectedHRId);
                        zzgtvsSelectedHR.ConvertToHighResolution();
                        zzgtvsSelected.ConvertToHighResolution();
                        foreach (var t in selected_gtvList)
                        {
                            Structure hrTarget = null;
                            var hrId = string.Format("zz{0}_HR", Helpers.ProcessStructureId(t.Id.ToString().Replace(" ", ""), MAX_ID_LENGTH - 5));
                            // remove if already there
                            Helpers.RemoveStructure(ss, hrId);

                            // add empty zopti total structure
                            hrTarget = ss.AddStructure(OPTI_DICOM_TYPE, hrId);
                            hrTarget.SegmentVolume = t.SegmentVolume;
                            if (hrTarget.CanConvertToHighResolution()) { hrTarget.ConvertToHighResolution(); } // TODO: may need to check if t is HR and then convert hrTarget to HR first?
                            zzgtvsSelectedHR.SegmentVolume = zzgtvsSelectedHR.Or(hrTarget.SegmentVolume);
                            Helpers.RemoveStructure(ss, hrId);
                        }
                        //zgtvTotalHR.SegmentVolume = Helpers.CropOutsideBodyWithMargin(zgtvTotalHR, bodyHR, 0);
                        zzgtvsSelected.SegmentVolume = zzgtvsSelectedHR.SegmentVolume;
                        Helpers.RemoveStructure(ss, zzgtvSelectedHRId);
                        MESSAGES += string.Format("\r\n\t-  Structure Booleaned: {0}", zzgtvSelectedId);
                        MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} 3D-parts", zzgtvSelectedId, zzgtvsSelected.GetNumberOfSeparateParts());
                        if (applyMarginGTV_CB.IsChecked == true & !GTVmargin_TextBox.Text.Equals("0") & !GTVmargin_TextBox.Text.Equals(""))
                        {
                            double gtvUnionMargin;
                            //Structure unionGTVplus = ss.AddStructure(GTV, zzgtvSelectedId+"_");
                            // set grow margin
                            if (GTVmargin_TextBox.Text == "" || string.IsNullOrWhiteSpace(GTVmargin_TextBox.Text)) { gtvUnionMargin = DEFAULT_AVOIDANCE_GROW_MARGIN; }
                            else
                            {
                                if (double.TryParse(GTVmargin_TextBox.Text, out gtvUnionMargin))
                                {
                                    //parsing successful 
                                }
                                else
                                {
                                    //parsing failed. 
                                    gtvUnionMargin = 0;
                                    MessageBox.Show(string.Format("Oops, an invalid value was used for the avoidance structure grow margin ({0}). The DEFAULT of {1} mm will be used.", GTVmargin_TextBox.Text, 0));
                                }
                            }
                            zzgtvsSelected.SegmentVolume = zzgtvsSelected.Margin(gtvUnionMargin);
                            //Helpers.RemoveStructure(ss, s);
                            MESSAGES += string.Format("\r\n\t-  Margin of {1}mm applied to: {0}", zzgtvSelectedId, GTVmargin_TextBox.Text);
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzgtvSelectedId, zzgtvsSelected.GetNumberOfSeparateParts());
                        }
                        else
                        {
                            MESSAGES += string.Format("\r\n\t-  Structure {0} consists of {1} separate volumes", zzgtvSelectedId, zzgtvsSelected.GetNumberOfSeparateParts());
                        }
                    }
                }

                #endregion UnionHelper

                #region clean up structure set

                foreach (var s in allCurrent_structures.Where(x => x.Contains("_HR")))
                {
                    try
                    {
                        Helpers.RemoveStructure(ss, s);
                        //MESSAGES += string.Format("\r\n\t-  Structure Removed: {0}", s);
                    }
                    catch
                    {
                    }
                }

                if (KeepPTVunions_CB.IsChecked == false)
                {


                    //MESSAGES += string.Format("\r\n\t-  Structure Deleted: {0}", zptvTotalId);
                    Helpers.RemoveStructure(ss, zptvSelectedId);
                    if (zptvSelectedId != "")
                    {
                        //MESSAGES += string.Format("\r\n\t-  Structure Deleted: {0}", zptvSelectedId);
                    }

                }



                // remove temporary high res structures
                Helpers.RemoveStructure(ss, bodyHRId);
                //Helpers.RemoveStructure(ss, zptvSelectedId);
                Helpers.RemoveStructure(ss, zptvSelectedHRId);
                Helpers.RemoveStructure(ss, "zzzTEMP"); // keep!!! used when booleaning structures -- Helpers.BooleanStructures()
                Helpers.RemoveStructure(ss, "zzzTEMP2");
                Helpers.RemoveStructure(ss, "zz95_ISO");

                foreach (var t in sorted_ptvList)
                {
                    var tempId = Helpers.ProcessStructureId(t.Id.ToString(), MAX_ID_LENGTH - 5);
                    try
                    {
                        Helpers.RemoveStructure(ss, string.Format("zz{0}_HR", tempId));
                    }
                    catch
                    {
                    }
                    try
                    {
                        Helpers.RemoveStructure(ss, string.Format("zz{0}", tempId));
                    }
                    catch
                    {
                    }

                }

                #endregion clean up structure set

                MESSAGES += "\r\n\r\n\tNOTE: *** Denotes an issue occured during the task's process\r\n\r\n";
                FlexibleMessageBox.Show(MESSAGES, "General Steps Completed", System.Windows.Forms.MessageBoxButtons.OK);

            }
        }
        }

        #region can be ignored fpr this script
        #region avoid structure option events


        // event fired when avoid option selected/unselected - boolean all targets || create avoids for multiple targets
        private void HandleAssessmentOptionsSelection(object sender, RoutedEventArgs e)
        {
            if (selectAllptvs_CB.IsChecked == true)
            {
                PTVList_LV.SelectAll();
            }
            if (applyMarginPTV_CB.IsChecked==true)
            {
                PTVmargin_SP.Visibility = Visibility.Visible;
            }
            else
            {
                PTVmargin_SP.Visibility = Visibility.Collapsed;
            }

            if (selectAllctvs_CB.IsChecked == true)
            {
                CTVList_LV.SelectAll();
            }
            if (applyMarginCTV_CB.IsChecked == true)
            {
                CTVmargin_SP.Visibility = Visibility.Visible;
            }
            else
            {
                CTVmargin_SP.Visibility = Visibility.Collapsed;
            }

            if (selectAllgtvs_CB.IsChecked == true)
            {
                GTVList_LV.SelectAll();
            }
            if (applyMarginGTV_CB.IsChecked == true)
            {
                GTVmargin_SP.Visibility = Visibility.Visible;
            }
            else
            {
                GTVmargin_SP.Visibility = Visibility.Collapsed;
            }

        }
        private void HandleAvoidOptionsSelection(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;

            var booleanAll = "BooleanAllTargets_CB";
            var multipleAvoidTargets = "MultipleAvoidTargets_CB";
            var keepPTVunions = "KeepPTVunions_CB";

            if (cb.IsChecked == true)
            {
                if (cb.Name == booleanAll)
                {
                    MultipleAvoidTargets_CB.IsChecked = false;
                    MultipleAvoidTargets_SP.Visibility = Visibility.Collapsed;
                }
                if (cb.Name == multipleAvoidTargets)
                {
                    BooleanAllTargets_CB.IsChecked = false;
                    MultipleAvoidTargets_SP.Visibility = Visibility.Visible;

                }
            }
            if (cb.IsChecked == false)
            {
                if (cb.Name == booleanAll)
                {
                    MultipleAvoidTargets_CB.IsChecked = true;
                    MultipleAvoidTargets_SP.Visibility = Visibility.Visible;
                }
                if (cb.Name == multipleAvoidTargets)
                {
                    BooleanAllTargets_CB.IsChecked = true;
                    MultipleAvoidTargets_SP.Visibility = Visibility.Collapsed;
                }
            }

        }

        // event fired when different number of dose levels is defined
        private void HandleAvoidTargetCount(object sender, RoutedEventArgs e)
        {
            var radio1 = AvoidTarget1_Radio;
            var radio2 = AvoidTarget2_Radio;
            var radio3 = AvoidTarget3_Radio;
            var radio4 = AvoidTarget4_Radio;

            if (radio1.IsChecked == true)
            {
                AvoidTarget1_SP.Visibility = Visibility.Visible;
                AvoidTarget2_SP.Visibility = Visibility.Collapsed;
                AvoidTarget3_SP.Visibility = Visibility.Collapsed;
                AvoidTarget4_SP.Visibility = Visibility.Collapsed;
            }
            else if (radio2.IsChecked == true)
            {
                AvoidTarget1_SP.Visibility = Visibility.Visible;
                AvoidTarget2_SP.Visibility = Visibility.Visible;
                AvoidTarget3_SP.Visibility = Visibility.Collapsed;
                AvoidTarget4_SP.Visibility = Visibility.Collapsed;
            }
            else if (radio3.IsChecked == true)
            {
                AvoidTarget1_SP.Visibility = Visibility.Visible;
                AvoidTarget2_SP.Visibility = Visibility.Visible;
                AvoidTarget3_SP.Visibility = Visibility.Visible;
                AvoidTarget4_SP.Visibility = Visibility.Collapsed;
            }
            else if (radio4.IsChecked == true)
            {
                AvoidTarget1_SP.Visibility = Visibility.Visible;
                AvoidTarget2_SP.Visibility = Visibility.Visible;
                AvoidTarget3_SP.Visibility = Visibility.Visible;
                AvoidTarget4_SP.Visibility = Visibility.Visible;
            }
        }


        #endregion avoid structure option events

        #region opti structure section events

        // event fired when opti option selected/unselected - crop from body option || multiple dose levels option
        private void HandleOptiOptionsSelection(object sender, RoutedEventArgs e)
        {
            // if any opti structure options checked : show the options section
            if (CropFromBody_CB.IsChecked == true || MultipleDoseLevels_CB.IsChecked == true || CreateCI_CB.IsChecked == true || CreateR50_CB.IsChecked == true)
            {
                if (OptiOptions_SP.Visibility == Visibility.Collapsed) { OptiOptions_SP.Visibility = Visibility.Visible; }
            }

            // if crop or multi dose levels checked
            if (CropFromBody_CB.IsChecked == true || MultipleDoseLevels_CB.IsChecked == true)
            {

                // show section if checked
                CropFromBody_SP.Visibility = CropFromBody_CB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                //CropFromOptis_SP.Visibility = MultipleDoseLevels_CB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                MultiDoseLevelOptions_SP.Visibility = MultipleDoseLevels_CB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                // show options if either is checked
                if (CropOptions_SP.Visibility == Visibility.Collapsed) { CropOptions_SP.Visibility = Visibility.Visible; }

            }

            // if crop and multi dose levels unchecked
            if (CropFromBody_CB.IsChecked == false && MultipleDoseLevels_CB.IsChecked == false)
            {
                if (CropOptions_SP.Visibility == Visibility.Visible) { CropOptions_SP.Visibility = Visibility.Collapsed; }

                // collapese sections
                CropFromBody_SP.Visibility = Visibility.Collapsed;
                //CropFromOptis_SP.Visibility = Visibility.Collapsed;
                MultiDoseLevelOptions_SP.Visibility = Visibility.Collapsed;
            }

            // if create ci or r50 structure checked
            if (CreateCI_CB.IsChecked == true || CreateR50_CB.IsChecked == true)
            {
                // reveal/hide options first
                CI_R50_SP.Visibility = CreateCI_CB.IsChecked == true || CreateR50_CB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                CIMargin_SP.Visibility = CreateCI_CB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                R50Margin_SP.Visibility = CreateR50_CB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

                // then show options if either is checked
                if (OptiOptions_SP.Visibility == Visibility.Collapsed) { OptiOptions_SP.Visibility = Visibility.Visible; }
            }



            // if none of the options are checked
            if (CropFromBody_CB.IsChecked == false && MultipleDoseLevels_CB.IsChecked == false && CreateCI_CB.IsChecked == false && CreateR50_CB.IsChecked == false)
            {
                // show options if either is checked
                OptiOptions_SP.Visibility = Visibility.Collapsed;

                // collapese sections
                CropFromBody_SP.Visibility = Visibility.Collapsed;
                //CropFromOptis_SP.Visibility = Visibility.Collapsed;
                MultiDoseLevelOptions_SP.Visibility = Visibility.Collapsed;
                CI_R50_SP.Visibility = Visibility.Collapsed;
                CIMargin_SP.Visibility = Visibility.Collapsed;
                R50Margin_SP.Visibility = Visibility.Collapsed;
            }
            if (CropFromBody_CB.IsChecked == false)
            {
                // show options if either is checked
                //OptiOptions_SP.Visibility = Visibility.Collapsed;

                // collapese sections
                CropFromBody_SP.Visibility = Visibility.Collapsed;
            }
            if (CreateCI_CB.IsChecked == false && CreateR50_CB.IsChecked == false)
            {
                // show options if either is checked
                //OptiOptions_SP.Visibility = Visibility.Collapsed;

                // collapese sections
                //CropFromBody_SP.Visibility = Visibility.Collapsed;
                //CropFromOptis_SP.Visibility = Visibility.Collapsed;
                //MultiDoseLevelOptions_SP.Visibility = Visibility.Collapsed;
                CI_R50_SP.Visibility = Visibility.Collapsed;
                CIMargin_SP.Visibility = Visibility.Collapsed;
                R50Margin_SP.Visibility = Visibility.Collapsed;
            }
        }

        private void HandlePlanOptionsSelection(object sender, RoutedEventArgs e)
        {
            
        }

        // event fired when different number of dose levels is defined
        private void HandleDoseLevelCount(object sender, RoutedEventArgs e)
        {
            var radio1 = DoseLevel1_Radio;
            var radio2 = DoseLevel2_Radio;
            var radio3 = DoseLevel3_Radio;
            //var radio4 = DoseLevel4_Radio;

            if (radio1.IsChecked == true)
            {
                DoseLevel1_SP.Visibility = Visibility.Visible;
                DoseLevel2_SP.Visibility = Visibility.Collapsed;
                DoseLevel3_SP.Visibility = Visibility.Collapsed;
                DoseLevel4_SP.Visibility = Visibility.Collapsed;
            }
            else if (radio2.IsChecked == true)
            {
                DoseLevel1_SP.Visibility = Visibility.Visible;
                DoseLevel2_SP.Visibility = Visibility.Visible;
                DoseLevel3_SP.Visibility = Visibility.Collapsed;
                DoseLevel4_SP.Visibility = Visibility.Collapsed;
            }
            else if (radio3.IsChecked == true)
            {
                DoseLevel1_SP.Visibility = Visibility.Visible;
                DoseLevel2_SP.Visibility = Visibility.Visible;
                DoseLevel3_SP.Visibility = Visibility.Visible;
                DoseLevel4_SP.Visibility = Visibility.Collapsed;
            }
            /*else if (radio4.IsChecked == true)
            {
              DoseLevel1_SP.Visibility = Visibility.Visible;
              DoseLevel2_SP.Visibility = Visibility.Visible;
              DoseLevel3_SP.Visibility = Visibility.Visible;
              DoseLevel4_SP.Visibility = Visibility.Visible;
            }*/
        }



        // not used
        #region not used

        //// event fired when opti option selected/unselected - crop from body option
        //private void CropFromBody_CB_Click(object sender, RoutedEventArgs e)
        //{
        //  var cb = sender as CheckBox;

        //  if (cb.IsChecked == true) { cropFromBody = true; CropFromBody_SP.Visibility = Visibility.Visible; }
        //  else { cropFromBody = false; CropFromBody_SP.Visibility = Visibility.Collapsed; }


        //}

        //// event fired when opti option selected/unselected - multiple dose levels option
        //private void MultipleDoseLevels_CB_Click(object sender, RoutedEventArgs e)
        //{
        //  var cb = sender as CheckBox;
        //  if (cb.IsChecked == true) { MultiDoseLevelOptions_SP.Visibility = Visibility.Visible; }
        //  else { MultiDoseLevelOptions_SP.Visibility = Visibility.Collapsed; }
        //}

        //private void CreateOptiGTV_CB_Click(object sender, RoutedEventArgs e)
        //{
        //  var cb = sender as CheckBox;
        //  if (cb.IsChecked == true) { createOptiGTVForSingleLesion = true; }
        //  else { createOptiGTVForSingleLesion = false; }
        //}

        //private void CreateOptiTotal_CB_Click(object sender, RoutedEventArgs e)
        //{
        //  var cb = sender as CheckBox;
        //  if (cb.IsChecked == true) { createOptiTotal  = true; }
        //  else { createOptiTotal = false; }
        //}

        #endregion not used

        #endregion opti structure section events
        #endregion
        #endregion button / checkbox events

        #endregion event controls
        //---------------------------------------------------------------------------------
        #region helper methods
        void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null)
                textBox.SelectAll();
        }

        // methods used in even handlers
        public void LogUser(string script)
        {
            #region User Stats

            // add headers if the file doesn't exist
            // list of target headers for desired dose stats
            // in this case I want to display the headers every time so i can verify which target the distance is being measured for
            // this is due to the inconsistency in target naming (PTV1/2 vs ptv45/79.2) -- these can be removed later when cleaning up the data
            if (!File.Exists(userLogPath))
            {
                List<string> dataHeaderList = new List<string>();
                dataHeaderList.Add("User");
                dataHeaderList.Add("PC");
                dataHeaderList.Add("Script");
                dataHeaderList.Add("Version");
                dataHeaderList.Add("Date");
                dataHeaderList.Add("DayOfWeek");
                dataHeaderList.Add("Time");
                dataHeaderList.Add("PatientID");
                dataHeaderList.Add("StructureSetId");
                dataHeaderList.Add("PlanID");
                //dataHeaderList.Add("RandomID");
                dataHeaderList.Add("CourseID");
                
                

                string concatDataHeader = string.Join(",", dataHeaderList.ToArray());

                userLogCsvContent.AppendLine(concatDataHeader);
            }

            List<object> userStatsList = new List<object>();

            var culture = new System.Globalization.CultureInfo("de-DE");
            var day2 = culture.DateTimeFormat.GetDayName(DateTime.Today.DayOfWeek);
            string planId;
            string course;
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            string pc = Environment.MachineName.ToString();
            string domain = Environment.UserDomainName.ToString();
            string userId = user.Replace(",", "");
            string scriptId = script.Replace(",", "");
            string date = string.Format("{0}-{1}-{2}", year, month, day);
            string dayOfWeek = day2;
            string time = string.Format("{0}:{1}", hour, minute);
            string ptId = id.Replace(",", "");
            string structureSetId = ss.Id.Replace(",","");
            try
            {
                planId = planSetup.Id.Replace(",", "");
                course = courseName.Replace(",", "");
            }
            catch
            {
                planId = "noPlan";
                course = "noCourse";
            }
            

            userStatsList.Add(userId);
            userStatsList.Add(pc);
            userStatsList.Add(scriptId);
            userStatsList.Add(version);
            userStatsList.Add(date);
            userStatsList.Add(dayOfWeek);
            userStatsList.Add(time);
            userStatsList.Add(ptId);
            //userStatsList.Add(randomPtId);
            userStatsList.Add(structureSetId);
            userStatsList.Add(planId);
            userStatsList.Add(course);
            

            string concatUserStats = string.Join(",", userStatsList.ToArray());

            userLogCsvContent.AppendLine(concatUserStats);


            #endregion Target Dose Stats

            #region Write Files

            File.AppendAllText(userLogPath, userLogCsvContent.ToString());

            #endregion

            
        }

        #endregion helper methods
        //---------------------------------------------------------------------------------
    }
   
   
}
