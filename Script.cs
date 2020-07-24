using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using JR.Utils.GUI.Forms;

//This script is based on a GitHub version from mtparagon5 from 20200227 (https://github.com/mtparagon5/ESAPI-Projects/tree/master/Projects/v15/OptiAssistant) but highly manipulated and more features are added

// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]


namespace VMS.TPS
{
  public class Script
  {
    public Script()
    {
    }

    public void Execute(ScriptContext context, Window window)
    {

            //---------------------------------------------------------------------------------
            #region plan context, maincontrol, and window defitions

            #region context variable definitions
            //Hospital hospital;
            string hospital = context.Patient.Hospital.Id;
            //StructureSet structureSet = context.StructureSet;
            PlanSetup planSetup = null;
            PlanSum psum = null;
            string pId = context.Patient.Id;
            StructureSet structureSet = null;
            List<double> rx_doses;
            double rxSum = 0;

            #region openScriptOrnot
            if (context.StructureSet == null && context.PlanSumsInScope.Count() == 0)
            {
                MessageBox.Show("Oops, there doesn't seem to be an active StructureSet.", "Start-Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (context.StructureSet == null && context.PlanSumsInScope.Count() > 1)
            {
                MessageBox.Show("This script will work best with only one plansum open at a time.\n\nPlease open only 1 plansum.", "Start-Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (context.StructureSet == null && context.PlanSumsInScope.Count() == 1)
            {
                psum = context.PlanSumsInScope.First();
                structureSet = psum.StructureSet;
                //MessageBox.Show(string.Format("structureSetID: {0}\npsumID: {1}", structureSet.Id, psum.Id), "Start-Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                //return;
                rx_doses = new List<double>();
                foreach (PlanSetup ps in psum.PlanSetups)
                {
                    try
                    {
                        rx_doses.Add(ps.TotalDose.Dose);
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("One of the prescriptions for the plansum is not defined");
                        return;
                    }
                }
                rxSum = rx_doses.Sum();
            }
            else
            {
                structureSet = context.StructureSet;
                planSetup = context.PlanSetup;
            }
            #endregion openScriptOrnot

         
      ProcessIdName.getRandomId(pId, out string rId);
      string course = context.Course != null ? context.Course.Id.ToString().Replace(" ", "_") : "NA";
      string pName = ProcessIdName.processPtName(context.Patient.Name);
      
      List<Structure> sorted_zptvList = new List<Structure>();

            /*var ptvDetails = new[]
            {
                new AllPTVsDetails {Artist = "a1", ArtistName = "a2", SongName = "a3"},
                new AllPTVsDetails {Artist = "b1", ArtistName = "b2", SongName = "b3"}
            };*/
            List<ptvsInitial> ptvDetails = new List<ptvsInitial>();
            List<planInitial> planDetails = new List<planInitial>();
            


            #endregion
            //---------------------------------------------------------------------------------
            #region window definitions

            // Add existing WPF control to the script window.
            var mainControl = new OptiAssistant.MainControl();
            
            mainControl.zwindow = window;
      //window.WindowStyle = WindowStyle.None;
      window.Content = mainControl;
      window.SizeToContent = SizeToContent.WidthAndHeight;
      window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      window.Title = "ContourUnions by MG (vers.1)";
           // window.Topmost = true;
            //window.Activate();

      #endregion
      //---------------------------------------------------------------------------------
      #region mainControl variable definitions

                foreach (Structure z in structureSet.Structures.OrderBy(x => x.Id).Where(s => !s.IsEmpty &! s.Id.Contains("_Saum")))
                {
                //if (z.Id.StartsWith(mainControl.OptiPrefix_TextBox.Text.ToString()))
                        if (z.Id.StartsWith(mainControl.OptiPrefix_TextBox.Text.ToString()) || z.Id.StartsWith("zIROS_PTV"))
                        {
                            sorted_zptvList.Add(z);
                        }
                }

        
            //mainControl.songDetails = songDetails;

            mainControl.ss = structureSet;
      mainControl.patient = context.Patient;
      mainControl.planSetup = planSetup;
      mainControl.psum = psum;
      mainControl.rxSum = rxSum;
            //inControl.SptvDetails =  SptvDetails;
            //mainControl.psumCount = context.PlanSumsInScope.Count(); 
            mainControl.user = context.CurrentUser.ToString();
      mainControl.day = DateTime.Now.ToString("dd");
      mainControl.month = DateTime.Now.ToString("MM");
      mainControl.year = DateTime.Now.ToString("yyyy");
      mainControl.hour = DateTime.Now.ToLocalTime().ToString("HH");
      mainControl.minute = DateTime.Now.ToLocalTime().ToString("mm");
      mainControl.timeStamp = string.Format("{0}", DateTime.Now.ToLocalTime().ToString());
      mainControl.curredLastName = context.Patient.LastName.Replace(" ", "_");
      //mainControl.curredFirstName = context.Patient.FirstName.Replace(" ", "_");
      //mainControl.firstInitial = context.Patient.FirstName[0].ToString();
      mainControl.lastInitial = context.Patient.LastName[0].ToString();
      //mainControl.initials = mainControl.firstInitial + mainControl.lastInitial;
      mainControl.id = pId;
      //mainControl.idAsDouble = Convert.ToDouble(mainControl.id);
      mainControl.randomId = rId;
      mainControl.courseName = course;

            // isGrady -- they don't have direct access to S Drive (to write files)
            /*var is_grady = MessageBox.Show("Are you accessing this script from the Grady Campus AND/OR from an Eclipse TBox?", "Direct $S Drive Access", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (is_grady == MessageBoxResult.Yes)
            {
              mainControl.isGrady = true;
            }
            else { mainControl.isGrady = false; }*/

        #endregion

        #endregion
        //---------------------------------------------------------------------------------
        #region structure listviews

        //---------------------------------------------------------------------------------
        #region organize structures into ordered lists
        // lists for structures
        if (structureSet != null)
        {
            GenerateStructureList.cleanAndOrderStructures(structureSet, out mainControl.sorted_gtvList,
                                                                        out mainControl.sorted_ctvList,
                                                                        out mainControl.sorted_itvList,
                                                                        out mainControl.sorted_ptvList,
                                                                        out mainControl.sorted_targetList,
                                                                        out mainControl.sorted_oarList,
                                                                        out mainControl.sorted_structureList,
                                                                        out mainControl.sorted_emptyStructuresList);
                

        }
        else
        {
                
        }


        //populate AllPTVs QuickInfo
            
        


      #endregion structure organization and ordering
      //---------------------------------------------------------------------------------
      #region populate listviews

      // warn user if there are High Res Structures
      mainControl.highresMessage = string.Empty;
      foreach (var s in mainControl.sorted_structureList)
      {
        if (s.IsHighResolution) { mainControl.highresMessage += string.Format("- {0}\r\n\t", s.Id); mainControl.hasHighRes = true; }
      }
      foreach (var t in mainControl.sorted_ptvList)
      {
        if (t.IsHighResolution) { mainControl.needHRStructures = true; }
      }

      if (mainControl.hasHighRes)
      {
        //MessageBox.Show(string.Format("The Following Are High Res Structures:\r\n\t{0}\r\n\r\nSometimes there can be issues when High Res Structures are involved.", mainControl.highresMessage));
      }


      #region not important in this script (is utilized in another script)
            // populate option listviews
            if (mainControl.sorted_ptvList.Count() < 1)
      {
        //MessageBox.Show("There are no PTVs detected. The tools for Opti PTV and Ring Creation are disabled.");
        mainControl.CreateOptis_CB.IsEnabled = false;
       
        mainControl.hasNoPTV = true;

        mainControl.BooleanAllTargets_CB.IsEnabled = false;
        mainControl.MultipleAvoidTargets_CB.IsEnabled = false;
      }
      else if (mainControl.sorted_ptvList.Count() == 1)
      {
        mainControl.MultipleDoseLevels_CB.IsEnabled = false;
        mainControl.hasSinglePTV  = true;

        // check and disable boolean all targets cb && disable multiple avoid targets cb
        mainControl.BooleanAllTargets_CB.IsChecked = true;
        mainControl.BooleanAllTargets_CB.IsEnabled = false;
        mainControl.MultipleAvoidTargets_CB.IsEnabled = false;
      }
      else
      {
        mainControl.CropAvoidsFromPTVs.Visibility = Visibility.Visible;
        mainControl.hasMultiplePTVs = true;
        mainControl.DoseLevel1_Radio.IsChecked = true;
        mainControl.BooleanAllTargets_CB.IsChecked = true;

        foreach (var s in mainControl.sorted_ptvList)
        {
          mainControl.DoseLevel1_Combo.Items.Add(s.Id); mainControl.AvoidTarget1_Combo.Items.Add(s.Id);
          mainControl.DoseLevel2_Combo.Items.Add(s.Id); mainControl.AvoidTarget2_Combo.Items.Add(s.Id);
          mainControl.DoseLevel3_Combo.Items.Add(s.Id); mainControl.AvoidTarget3_Combo.Items.Add(s.Id);
          //mainControl.DoseLevel4_Combo.Items.Add(s.Id); mainControl.AvoidTarget4_Combo.Items.Add(s.Id);
        }
      }
            #endregion

      // populate listviews with structures on startup
      if (mainControl.sorted_oarList != null) { foreach (Structure s in mainControl.sorted_oarList) { mainControl.OarList_LV.Items.Add(s.Id); } }
      if (mainControl.sorted_ptvList != null) { foreach (Structure t in mainControl.sorted_ptvList) { mainControl.PTVList_LV.Items.Add(t.Id); } }
      if (mainControl.sorted_ctvList != null) { foreach (Structure t in mainControl.sorted_ctvList) { mainControl.CTVList_LV.Items.Add(t.Id); } }
      if (mainControl.sorted_gtvList != null) { foreach (Structure t in mainControl.sorted_gtvList) { mainControl.GTVList_LV.Items.Add(t.Id); } }
            //if (sorted_zptvList != null) { foreach (Structure t in sorted_zptvList) { mainControl.PTVListForRings_LV.Items.Add(t.Id); } }

        // populate all ptv overview
        mainControl.ListViewAllPTVstart.ItemsSource = ptvDetails;

        // populate plan overview
        mainControl.ListViewPlan.ItemsSource = planDetails;

            
     
      #endregion
      //---------------------------------------------------------------------------------

      #endregion
      //---------------------------------------------------------------------------------
      #region data populated on startup

      //---------------------------------------------------------------------------------
      #region log

      if (mainControl.isGrady == false)
        {
          mainControl.LogUser(mainControl.script);
        }

      #endregion
            //---------------------------------------------------------------------------------

            #endregion
            //---------------------------------------------------------------------------------
       
    }
        public class ptvsInitial
        {
            public string ptvID { get; set; }

            public double ptvVolume { get; set; }

            public double ptvD98 { get; set; }

            public double ptvDisIso { get; set; }
        }
        public class planInitial
        {
            public string planID { get; set; }

            public double fractions { get; set; }
           // public int beams { get; set; }
            public double totalMU { get; set; }
            public int tables { get; set; }
            public double totalDose { get; set; }
        }
        
    }
}
