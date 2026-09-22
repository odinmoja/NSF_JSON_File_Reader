
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using NSF_JSON_Reader.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;



namespace NSF_JSON_Reader
{

    public partial class FrmHome : Form
    {
        String directoryPath = "";
       

        public FrmHome()
        {
            InitializeComponent();

        }

        private void FrmHome_Load(object sender, EventArgs e)
        {

        }


        private void btnLoad_Click(object sender, EventArgs e)
        {
            int numFolders;
            int numFiles = 0;
            string currentFileBeingProcessed = "";


            try
            {
                directoryPath = txtPath.Text;


                //Don't need sub  folder search at this point will expand later
                //List<string> dirs = new List<string>(Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories));

                //In the event the user tried to load folder without giving a filepath the code must gracefully handle it.
                if (directoryPath == String.Empty)
                {
                    MessageBox.Show("Please ensure you enter a file path");
                    return;
                }

                if (CheckDBFields() == false)
                {
                    MessageBox.Show("Please ensure all the database connection fields are filled");
                    return;
                }

                List<string> dirs = new List<string>(Directory.EnumerateDirectories(directoryPath));

                //In the case that there are no sub folders ust use the path given by the user
                if (dirs.Count == 0)
                {
                    dirs.Add(directoryPath);
                }

                //Let the users know how many folders were found at the specified location.
                numFolders = dirs.Count;
                txtFolders.Text = numFolders.ToString();



                String nsfDBConnectString = "Server=" + txtServer.Text +
                                            ";Database=" + txtDBName.Text +
                                            ";User ID=" + txtUserID.Text +
                                            ";Password=" + txtPassword.Text + ";";
                var context = new ApplicationDbContext(nsfDBConnectString);

                /*Ok, the  goal here is to have a mechanism to check against the db for existing awards.
                 This collection of ids  will save trips to the db and by calling only the ward ids we
                save memory as opposed to fetching the whole entity. This will come in handy once we start
                iterating through files and folders*/
                var existingAwardIds = context.Awards.Select(a => a.NSFAwdId).ToHashSet();

                /*pretty much the same thing as above but for institutions but we need to doa little extra work for 
                 the Pk-FK stuff when attachign awards to existing institutions.*/

                /*var existingInstitutionsDict = context.Institutions
                     .Where(i => !String.IsNullOrEmpty(i.UeiNumber))
                    .Select(i => new { i.UeiNumber, i.InstitutionId })
                        .AsNoTracking()
                    .ToDictionary(i => i.UeiNumber, i => i.InstitutionId); */

                /* var existingInstitutions = context.Institutions
                     .Where(i => !string.IsNullOrEmpty(i.UeiNumber)
                      &&  String.IsNullOrEmpty(i.PerformanceCountryFlag)
                      && String.IsNullOrEmpty(i.PerformanceCountryName))
                     .ToDictionary(i => i.UeiNumber, i => i); */

                var pendingInstitutions = new Dictionary<string, Entities.Institution>();

                int batchSize =  (int)nmbBox1.Value;
                int batchcount = 0;
                foreach (string dir in dirs)
                {
                    DirectoryInfo myDirectory = new DirectoryInfo(dir);
                    List<Entities.Award> folderProjects = new List<Award>();

                    foreach (FileInfo myFile in myDirectory.GetFiles("*.json"))
                    {
                        currentFileBeingProcessed = myFile.FullName;
                        string jsonString = File.ReadAllText(myFile.FullName);
                        /* The reason I am going the excruciating  get property route is because my data models  are not a 1:1 
                          match with the json. For instance, instead of having primary investigator and program manager, 
                        I use the person model to record both instances of information.
                        The rationale being this will make it easier when constructing tables
                        but also easier when persuing 
                        the project goals  of:
                        Who was involved
                        Which institutions
                        What was it about
                        What did it produce
                        what fields converged*/
                  

                        Entities.Award myAward = new Award();

                        JsonDocument doc = JsonDocument.Parse(jsonString);
                        //General award information
                        myAward.NSFAwdId = doc.RootElement.GetProperty("awd_id").GetString();



                        if (existingAwardIds.Contains(myAward.NSFAwdId))
                        {
                       
                         //   continue;
                           
                        }

                        myAward.AgencyId = doc.RootElement.GetProperty("agcy_id").GetString();
                        myAward.TranType = doc.RootElement.GetProperty("tran_type").GetString();
                        myAward.AwardInstrumentText = doc.RootElement.GetProperty("awd_istr_txt").GetString();
                        myAward.OrgCode = doc.RootElement.GetProperty("org_code").GetString();
                        myAward.DirectorateAbbreviation = doc.RootElement.GetProperty("dir_abbr").GetString();
                        myAward.OrgDirectorateLongName = doc.RootElement.GetProperty("org_dir_long_name").GetString();
                        myAward.DivisionAbbreviation = doc.RootElement.GetProperty("div_abbr").GetString();
                        myAward.OrgDivisionLongName = doc.RootElement.GetProperty("org_div_long_name").GetString();
                        myAward.AwdAgcyCode = doc.RootElement.GetProperty("awd_agcy_code").GetString();
                        myAward.FundAgcyCode = doc.RootElement.GetProperty("fund_agcy_code").GetString();

                        //Financial and management information
                        myAward.CfdaNumber = doc.RootElement.GetProperty("cfda_num").GetString();
                        myAward.Title = doc.RootElement.GetProperty("awd_titl_txt").GetString();
                        myAward.IntendedAmount = doc.RootElement.GetProperty("tot_intn_awd_amt").GetDecimal();
                        myAward.AwardAmount = doc.RootElement.GetProperty("awd_amount").GetDecimal();
                        myAward.StartDate = Convert.ToDateTime(doc.RootElement.GetProperty("awd_eff_date").GetString());
                        myAward.EndDate = Convert.ToDateTime(doc.RootElement.GetProperty("awd_exp_date").GetString());
                        myAward.AbstractNarration = doc.RootElement.GetProperty("awd_abstract_narration").GetString();

                        myAward.ProgramManager = new Entities.Person();
                        myAward.ProgramManager.FullName = doc.RootElement.GetProperty("po_sign_block_name").GetString();
                        myAward.ProgramManager.PhoneNumber = doc.RootElement.GetProperty("po_phone").GetString();
                        myAward.ProgramManager.EmailAddress = doc.RootElement.GetProperty("po_email").GetString();
                        myAward.ProgramManager.Role = "Program Manager";




                        string ueiNumber = doc.RootElement.GetProperty("inst").GetProperty("org_uei_num").GetString();
                        myAward.AwardeeName = doc.RootElement.GetProperty("inst").GetProperty("inst_name").GetString();
                        myAward.AwardeeStateCode = doc.RootElement.GetProperty("inst").GetProperty("inst_state_code").GetString();

                        /*This optimisation should probably work. The only problem would be if i tried to access any
                         of the institution properties of myAward because it will only be loaded once a savechanges is called.
                        However, this  never happens, in the case that it does exist will will just set the FK because we are
                        primarily concerned with the navigation property. If it does not exist then we are setting all those properties
                        from JSON  anyway.*/


                        Entities.Institution existingInstitution = null;


                        if(!String.IsNullOrEmpty(ueiNumber))
                        {
                            if (pendingInstitutions.TryGetValue(ueiNumber, out var pendingInstitution))
                            {
                                existingInstitution = pendingInstitution;
                            }
                            else
                            {

                                existingInstitution = context.Institutions
                                    .FirstOrDefault(i => i.UeiNumber == ueiNumber);
                            }

                        }
                        


                    

                        if (existingInstitution != null)
                        {
                            myAward.Institution = existingInstitution;
                            myAward.AwardeeName = existingInstitution.InstitutionName;
                            myAward.AwardeeStateCode = existingInstitution.StateCode;
                        }
                        else
                        {

                            //Populate fields for the institution
                            myAward.Institution = new Entities.Institution();
                            myAward.Institution.UeiNumber = doc.RootElement.GetProperty("inst").GetProperty("org_uei_num").GetString();
                            myAward.Institution.ParentUeiNumber = doc.RootElement.GetProperty("inst").GetProperty("org_prnt_uei_num").GetString();

                            if (myAward.Institution.UeiNumber.Equals(""))
                            {
                                myAward.Institution.UeiNumber = null;
                            }

                            myAward.Institution.InstitutionName = doc.RootElement.GetProperty("inst").GetProperty("inst_name").GetString();
                            myAward.Institution.StreetAddress = doc.RootElement.GetProperty("inst").GetProperty("inst_street_address").GetString();
                            myAward.Institution.StreetAddress2 = doc.RootElement.GetProperty("inst").GetProperty("inst_name").GetString();
                            myAward.Institution.CityName = doc.RootElement.GetProperty("inst").GetProperty("inst_city_name").GetString();
                            myAward.Institution.StateCode = doc.RootElement.GetProperty("inst").GetProperty("inst_state_code").GetString();
                            myAward.Institution.StateName = doc.RootElement.GetProperty("inst").GetProperty("inst_state_name").GetString();
                            myAward.Institution.PhoneNum = doc.RootElement.GetProperty("inst").GetProperty("inst_phone_num").GetString();
                            myAward.Institution.ZipCode = doc.RootElement.GetProperty("inst").GetProperty("inst_zip_code").GetString();
                            myAward.Institution.CountryName = doc.RootElement.GetProperty("inst").GetProperty("inst_country_name").GetString();
                            myAward.Institution.CongressionalDistrictCode = doc.RootElement.GetProperty("inst").GetProperty("cong_dist_code").GetString();
                            myAward.Institution.StateCongressionalDistCode = doc.RootElement.GetProperty("inst").GetProperty("st_cong_dist_code").GetString();
                            myAward.Institution.CountryName = doc.RootElement.GetProperty("inst").GetProperty("inst_country_name").GetString();
                            myAward.Institution.PerformanceCountryFlag = null;
                            myAward.Institution.PerformanceCountryName = null;
                            if(!String.IsNullOrEmpty(ueiNumber))
                            {
                                pendingInstitutions.Add(ueiNumber, myAward.Institution);
                            }
                            
                        }




                        //Populate fields for performing institution.
                        myAward.PerfInstitution = new Entities.Institution();
                        myAward.PerfInstitution.InstitutionName = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_inst_name").GetString();
                        myAward.PerfInstitution.StreetAddress = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_str_addr").GetString();
                        myAward.PerfInstitution.StreetAddress2 = null;
                        myAward.PerfInstitution.CityName = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_city_name").GetString();
                        myAward.PerfInstitution.StateCode = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_st_code").GetString();
                        myAward.PerfInstitution.StateName = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_st_name").GetString();
                        myAward.PerfInstitution.PhoneNum = null;
                        myAward.PerfInstitution.ZipCode = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_zip_code").GetString();
                        myAward.PerfInstitution.PerformanceCountryName = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_ctry_name").GetString();
                        myAward.PerfInstitution.PerformanceCountryFlag = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_ctry_flag").GetString();
                        //myAward.PerfInstitution.countryCode = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_ctry_code").GetString();
                        myAward.PerfInstitution.CongressionalDistrictCode = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_cong_dist").GetString();
                        myAward.PerfInstitution.StateCongressionalDistCode = doc.RootElement.GetProperty("perf_inst").GetProperty("perf_st_cong_dist").GetString();
                        myAward.PerfInstitution.UeiNumber = null;
                        myAward.PerfInstitution.ParentUeiNumber = null;


                        var ProgramElements = new List<Entities.ProgramElement>();


                        if (doc.RootElement.TryGetProperty("pgm_ele", out JsonElement validElement) &&
                            validElement.ValueKind == JsonValueKind.Array)
                        {

                            foreach (JsonElement myProgramElementArray in doc.RootElement.GetProperty("pgm_ele").EnumerateArray())
                            {
                                Entities.ProgramElement myElement = new Entities.ProgramElement();
                                myElement.ProgramElementName = myProgramElementArray.GetProperty("pgm_ele_name").GetString();
                                myElement.ProgramElementCode = myProgramElementArray.GetProperty("pgm_ele_code").GetString();
                                ProgramElements.Add(myElement);
                            }
                        }

                        var ProgramReferences = new List<Entities.ProgramReference>();


                        if (doc.RootElement.TryGetProperty("pgm_ref", out JsonElement validRef) &&
                            validRef.ValueKind == JsonValueKind.Array)
                        {
                            //The array is not null go ahead and iterate through it

                            foreach (JsonElement myProgramReferenceArray in doc.RootElement.GetProperty("pgm_ref").EnumerateArray())
                            {
                                Entities.ProgramReference myReference = new Entities.ProgramReference();
                                myReference.ProgramReferenceCode = myProgramReferenceArray.GetProperty("pgm_ref_code").GetString();
                                myReference.ProgramReferenceName = myProgramReferenceArray.GetProperty("pgm_ref_txt").GetString();
                                ProgramReferences.Add(myReference);
                            }

                        }

                        var Investigators = new List<Entities.Person>();


                        if (doc.RootElement.TryGetProperty("pi", out JsonElement validPerson) &&
                        validPerson.ValueKind == JsonValueKind.Array)
                        {
                            //The array is not null go ahead and iterate through it

                            foreach (JsonElement myInvestigatorArray in doc.RootElement.GetProperty("pi").EnumerateArray())
                            {
                                Entities.Person myInvestigator = new Entities.Person();
                                myInvestigator.Role = myInvestigatorArray.GetProperty("pi_role").GetString();
                                myInvestigator.FirstName = myInvestigatorArray.GetProperty("pi_first_name").GetString();
                                myInvestigator.LastName = myInvestigatorArray.GetProperty("pi_last_name").GetString();
                                myInvestigator.MidInit = myInvestigatorArray.GetProperty("pi_mid_init").GetString();
                                myInvestigator.SufxName = myInvestigatorArray.GetProperty("pi_sufx_name").GetString();
                                myInvestigator.FullName = myInvestigatorArray.GetProperty("pi_full_name").GetString();
                                myInvestigator.EmailAddress = myInvestigatorArray.GetProperty("pi_email_addr").GetString();
                                myInvestigator.NsfId = myInvestigatorArray.GetProperty("nsf_id").GetString();
                                myInvestigator.StartDate = Convert.ToDateTime(myInvestigatorArray.GetProperty("pi_start_date").GetString());
                                String myEndDate = myInvestigatorArray.GetProperty("pi_end_date").GetString();
                                if (myEndDate != null && myEndDate.Length > 1)
                                {
                                    myInvestigator.EndDate = Convert.ToDateTime(myEndDate);
                                }
                                else
                                {
                                    myInvestigator.EndDate = null;
                                }

                                Investigators.Add(myInvestigator);
                            }

                        }

                        //This is a record of obligated funding to the award by year (if it is listed)
                        myAward.FinancialObligations = new List<Entities.FinancialObligationYear>();

                        if (doc.RootElement.TryGetProperty("oblg_fy", out JsonElement validObligationYear) &&
                            validObligationYear.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement myObligationArray in doc.RootElement.GetProperty("oblg_fy").EnumerateArray())
                            {
                                Entities.FinancialObligationYear myObligation = new Entities.FinancialObligationYear();
                                myObligation.FiscalYear = myObligationArray.GetProperty("fund_oblg_fiscal_yr").GetInt32();
                                myObligation.ObligatedAmount = myObligationArray.GetProperty("fund_oblg_amt").GetDouble();
                                myAward.FinancialObligations.Add(myObligation);
                            }

                        }

                        var AppFundSources = new List<Entities.PrimaryProgramFundingSource>();

                        if (doc.RootElement.TryGetProperty("app_fund", out JsonElement valiFundingSource) &&
                          valiFundingSource.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement myFundingSourceArray in doc.RootElement.GetProperty("app_fund").EnumerateArray())
                            {
                                Entities.PrimaryProgramFundingSource myObligation = new Entities.PrimaryProgramFundingSource();
                                myObligation.ApplicationCode = myFundingSourceArray.GetProperty("app_code").GetString();
                                myObligation.ApplicationName = myFundingSourceArray.GetProperty("app_name").GetString();
                                myObligation.ApplicationSymbId = myFundingSourceArray.GetProperty("app_symb_id").GetString();
                                myObligation.FundingCode = myFundingSourceArray.GetProperty("fund_code").GetString();
                                myObligation.FundingName = myFundingSourceArray.GetProperty("fund_name").GetString();
                                myObligation.FundingSymbId = myFundingSourceArray.GetProperty("fund_symb_id").GetString();

                                AppFundSources.Add(myObligation);
                            }

                        }

                        if (doc.RootElement.TryGetProperty("por", out JsonElement validProjectOutcome) &&
                      validProjectOutcome.ValueKind == JsonValueKind.Object)
                        {
                            myAward.ProjectOutcomesReport = doc.RootElement.GetProperty("por").GetProperty("por_txt_cntn").GetString();

                        }


                        context.Awards.Add(myAward);
                        //context.SaveChanges();
                        context.AwardPersons.Add(new AwardPerson
                        {
                            Award = myAward,
                            Person = myAward.ProgramManager
                        });
                        //context.SaveChanges();

                        //This section handles all thye linking table entities.

                        foreach (var investigator in Investigators)
                        {
                            context.AwardPersons.Add(new AwardPerson
                            {
                                Award = myAward,
                                Person = investigator

                            });
                        }

                        foreach (var pref in ProgramReferences)
                        {
                            context.AwardProgramReferences.Add(new AwardProgramReference
                            {
                                Award = myAward,
                                ProgramReference = pref
                            });
                        }
                        foreach (var pelem in ProgramElements)
                        {
                            context.AwardProgramElements.Add(new AwardProgramElement
                            {
                                Award = myAward,
                                ProgramElement = pelem
                            });
                        }
                        foreach (var appFundSource in AppFundSources)
                        {
                            context.AwardFundingSources.Add(new AwardFundingSource
                            {
                                Award = myAward,
                                FundingSource = appFundSource
                            });
                        }

                        context.AwardInstitutions.Add(new AwardInstitution
                        {
                            Award = myAward,
                            Institution = myAward.Institution

                        });

                        batchcount++;


                      //  context.SaveChanges();

                        //if (batchcount >= batchSize)
                        //{
                        //    context.SaveChanges();
                        //    context.Dispose();
                        //    context = new ApplicationDbContext(nsfDBConnectString);
                        //    batchcount = 0;
                        //    pendingInstitutions.Clear();
                        //}



                        //For  Logging

                        if (batchcount >= batchSize)
                        {                                           

                            try
                            {
                                context.SaveChanges();
                            }
                            catch (DbUpdateException ex)
                            {
                                string errorDetails =
                                    $"Outer: {ex.Message}\n" +
                                    $"Inner 1: {ex.InnerException?.Message}\n\n";
                                //$"Inner 2: {ex.InnerException?.InnerException?.Message}\n\n";
                                int totalFailedDBTransactions = 0;


                                foreach (var entry in ex.Entries)
                                {
                                    errorDetails += $"Problem entity: {entry.Entity.GetType().Name}\n";

                                    var nsfAwdId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "NSFAwdId");

                                    if (nsfAwdId != null)
                                    {
                                        errorDetails += $"  NSFAwdId: {nsfAwdId.CurrentValue}\n";
                                        errorDetails += "Check file named " + nsfAwdId.CurrentValue + ".JSON\n"; 
                                    }

                                    /*This goes through every property  of every entry. We don't need this now
                                      We just need the NSFAwdID since that is the name of each corresponding 
                                    JSON  file.*/
                                    //foreach (var prop in entry.Properties)
                                    //{
                                    //    errorDetails += $"  {prop.Metadata.Name}: {prop.CurrentValue}\n";
                                    //}
                                    errorDetails += "---\n";
                                    totalFailedDBTransactions++;
                                    if(totalFailedDBTransactions >= 10)
                                    {
                                        errorDetails += "Note:These are only the first 10 failed database transactions.\n";
                                        errorDetails += "Depending on batch size there could be 100s. Error reports truncated for brevity \n";
                                        errorDetails += "Actual total errors number: " + ex.Entries.Count;
                                        break;
                                    }
                                }
                                /*Ok I am making an executive design decision here.
                                 I am commenting out the filw writing logic and replacing it simply with a formatted
                                string which will be output  which will be shown in a mesage box. I am doing this 
                                for 2 reason.
                                1) It circumvents  dealing with file IO stuff and the complicatiosn that come with
                                that for now.
                                2) If a failure occurs Iw ant it to be loud and immediate to the user. Writing it
                                to text allows things to fail a little more quietly.*/

                                //File.AppendAllText(@"D:\MyLogs\error_log.txt", errorDetails);
                                //MessageBox.Show("Save failed - check C:\\MyLogs\\error_log.txt for details");
                                MessageBox.Show(errorDetails, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                throw;
                            }
                            context.Dispose();
                            context = new ApplicationDbContext(nsfDBConnectString);
                            batchcount = 0;
                            pendingInstitutions.Clear();
                        }

                    

                        folderProjects.Add(myAward);

                    }

                    context.SaveChanges();



                    numFiles += folderProjects.Count;
                    txtFiles.Text = numFiles.ToString();
                    MessageBox.Show("File loading complete", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }



            }

            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message +"\n" +"\n"
                    +"Error happened while processing file " + currentFileBeingProcessed  + "\n"
                    +"The culprit file name is only accurate if you chose 0 as a batch size or else it" +
                    "could be any file in the batch.");

            }
        }

        private bool CheckDBFields()
        {
            if (String.IsNullOrEmpty(txtServer.Text) || String.IsNullOrEmpty(txtDBName.Text) ||
                String.IsNullOrEmpty(txtUserID.Text) || String.IsNullOrEmpty(txtPassword.Text))
            {
                return false;
            }

            return true;
        }

        private void btnDBTest_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckDBFields() == false)
                {
                    MessageBox.Show("Please ensure all database cconnection fields are filled");
                    return;
                }

                String testDBConnectString = "Server="+ txtServer.Text +
                                            ";Database="+txtDBName.Text+
                                            ";User ID="+txtUserID.Text +
                                            ";Password="+txtPassword.Text+";";

                var testContext = new ApplicationDbContext(testDBConnectString);

                if (testContext.Database.CanConnect())
                {
                    MessageBox.Show("Database connection settings functional");
                }
                else
                {
                    MessageBox.Show("Could not find and connect to database with specified settings");
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show("Error: " + ex.Message);
            }

           
        }
    }
}

