using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Data;
using System.Web;
//using ba360lib.brokeragent360.leads;
//using ba360lib.com.coretechcrm;
using ba360lib.com.streamscrm;
using System.Collections.Specialized;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security.Membership;
using DotNetNuke.Common.Utilities;
using System.Xml;
using System.IO;
using DotNetNuke.Entities;
using DotNetNuke.Entities.Profile;
using System.Net.Mail;
using ba360lib.Mail;
using DotNetNuke.Entities.Portals;  
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Configuration;
using System.Net;  
using System.Configuration;
namespace ba360lib
{
    public class BA360_lib
    {
        private string _connString = System.Configuration.ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;
        
        //string UserName = "sdmsuper", Password = "coldfusion", URL = "http://leads.brokeragent360.com/service/v4/soap.php?wsdl";// "http://leads.brokeragent360.com/service/v4/soap.php?wsdl";
        //web_service_admin/streamsservice2016
        string UserName = "web_service_admin", Password = "streamsservice2016"; //URL = "http://streamscrm.com/custom/service/v4_custom/soap.php?wsdl";
        string URL = System.Web.Configuration.WebConfigurationManager.AppSettings["BackendSOAPurl"];
        string backimageurl = System.Web.Configuration.WebConfigurationManager.AppSettings["backimageurl"];
        string portalname = System.Web.Configuration.WebConfigurationManager.AppSettings["portalname"];
        PortalController portalCntrlr = new PortalController();

        public string SessionID { get; set; }

        public sugarsoap SugarClient { get; set; }

        public string BackendLeadID { get; set; }

        public string PennyLead { get; set; }

        public int BackendSuccess { get; set; }

        public string RecordID { get; set; }

        // public string DnnLeadID { get; set; }

        //reurns session id of saop call connection  

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string get_connection()
        {

            try
            {
                SugarClient = new sugarsoap();
                SugarClient.Timeout = 900000;
                SugarClient.Url = URL;

                //Create authentication object
                user_auth UserAuth = new user_auth();

                //Populate credentials
                UserAuth.user_name = UserName;
                UserAuth.password = getMD5(Password);

                //Try to authenticate
                name_value[] LoginList = new name_value[0];
                entry_value LoginResult = SugarClient.login(UserAuth, "SoapTest", LoginList);

                //check for session id
                if (LoginResult.id != String.Empty)
                {
                    //set session id
                    SessionID = LoginResult.id;
                }
                else
                {
                    SessionID = null;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return SessionID;
        }

        //getting list of agents from backend
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataTable get_agents_list(AgentInfo agent_info)
        {
            DataTable table;
            try
            {
                get_connection();
                var fieldsToRetrieve = new[] { "loanappurl_c", "twitter_url_c", "lender_company_c", "lender_company_logo_c", "linked_in_c", "phone_mobile", "phone_other", "tagline_c", "awards_c", "url_c", "email1", "show_on_site_c", "company_name_c", "users_pic_c", "id", "first_name", "last_name", "address_street", "address_city", "address_state", "address_postalcode", "address_country", "phone_work", "user_name", "description", "title", "facebook_url_c", "agentmls_c", "officemls1_c", "active_rotate_flag_c", "leadsperround_c", "current_rotate_cnt_c", "send_leads_c", "lead_flow_c", "roletype_c", "lender_to_agent_c", "youtube_url_c" };

                string condition = "show_on_site_c='" + agent_info.ShowOnSite + "' and roletype_c=" + agent_info.RoleType + " and company_name_c='" + agent_info.CompanyID + "'";

                link_name_to_fields_array[] entryList = new link_name_to_fields_array[0];

                var result = SugarClient.get_entry_list(SessionID, "Users", condition, "last_name asc", 0, fieldsToRetrieve, entryList, 250, 0, false);   
                table = new DataTable();

                for (int k = 0; k < fieldsToRetrieve.Length; k++)
                    table.Columns.Add(fieldsToRetrieve[k], typeof(string));

                for (int i = 0; i < result.result_count; i++)
                {
                    DataRow dtRow = table.NewRow();
                    for (int j = 0; j < fieldsToRetrieve.Length; j++)
                    {
                        if (fieldsToRetrieve[j].Equals("users_pic_c"))
                        {
                            string users_pic = result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                            if (users_pic != null && users_pic != "")
                                //old path
                                //dtRow[fieldsToRetrieve[j]] = "http://leads.brokeragent360.com/custom/SynoFieldPhoto/phpThumb/images/" + result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;

                                // new path 
                                dtRow[fieldsToRetrieve[j]] = backimageurl + result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;


                            else
                                dtRow[fieldsToRetrieve[j]] = "/images/default_users.jpg";
                        }
                        else
                            dtRow[fieldsToRetrieve[j]] = result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                    }
                    table.Rows.Add(dtRow);
                }
                SugarClient.logout(SessionID);
                return table;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;

        }

        public DataTable get_LeadList()
        {
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();

            try
            {
                using (SqlConnection con = new SqlConnection(_connString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Prc_GetLeadInfo";
                    cmd.Connection = con;

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    con.Open();
                    da.Fill(ds);
                    con.Close();
                    if (ds != null)
                    {
                        if (ds.Tables.Count > 0)
                        {
                            dt = ds.Tables[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return dt;
        }


        public string InsertLeadDetails(LeadInfo lead_info, string Handle)
        {
            string logfilepath = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + portalname + "/MLS_Medias/lead_activity_xml/Shedulers_log_Files/SendMailEx.txt");
            string IsSuccess = "False";
            try
            {

                using (SqlConnection con = new SqlConnection(_connString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Prc_LeadInfo";
                    cmd.Connection = con;

                    cmd.Parameters.Add("@Handle", SqlDbType.VarChar).Value = Handle;

                    cmd.Parameters.Add("@FirstName", SqlDbType.VarChar).Value = lead_info.FirstName;
                    cmd.Parameters.Add("@LastName", SqlDbType.VarChar).Value = lead_info.LastName;
                    cmd.Parameters.Add("@PhoneWork", SqlDbType.VarChar).Value = lead_info.PhoneWork;
                    cmd.Parameters.Add("@EmailID", SqlDbType.VarChar).Value = lead_info.EmailID;
                    cmd.Parameters.Add("@AgentID", SqlDbType.VarChar).Value = lead_info.AgentID;
                    cmd.Parameters.Add("@LeadSource", SqlDbType.VarChar).Value = lead_info.LeadSource;
                    cmd.Parameters.Add("@LenderID", SqlDbType.VarChar).Value = lead_info.LenderID;
                    cmd.Parameters.Add("@CampaignID", SqlDbType.VarChar).Value = lead_info.CampaignID;
                    cmd.Parameters.Add("@CampaignName", SqlDbType.VarChar).Value = lead_info.CampaignName;
                    cmd.Parameters.Add("@Description", SqlDbType.VarChar).Value = lead_info.Description;
                    cmd.Parameters.Add("@Status", SqlDbType.VarChar).Value = lead_info.Status;
                    cmd.Parameters.Add("@AgentWebsite", SqlDbType.VarChar).Value = lead_info.AgentWebsite;
                    cmd.Parameters.Add("@DnnLeadID", SqlDbType.VarChar).Value = lead_info.DnnLeadID;
                    cmd.Parameters.Add("@CompanyID", SqlDbType.VarChar).Value = lead_info.CompanyID;
                    cmd.Parameters.Add("@DnnPassword", SqlDbType.VarChar).Value = lead_info.DnnPassword;
                    cmd.Parameters.Add("@OpportunityAmount", SqlDbType.VarChar).Value = lead_info.OpportunityAmount;
                    cmd.Parameters.Add("@CreatedBY", SqlDbType.VarChar).Value = lead_info.CreatedBY;
                    cmd.Parameters.Add("@Comment", SqlDbType.VarChar).Value = lead_info.Comment;
                    cmd.Parameters.Add("@ActivityType", SqlDbType.VarChar).Value = lead_info.ActivityType;
                    cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = lead_info.Name;
                    cmd.Parameters.Add("@StatusForLender", SqlDbType.VarChar).Value = lead_info.StatusForLender;
                    cmd.Parameters.Add("@form_views_c", SqlDbType.VarChar).Value = lead_info.form_views_c;
                    cmd.Parameters.Add("@search_count_c", SqlDbType.VarChar).Value = lead_info.search_count_c;
                    cmd.Parameters.Add("@toemail", SqlDbType.VarChar).Value = lead_info.toemail;
                    cmd.Parameters.Add("@fromemail", SqlDbType.VarChar).Value = lead_info.fromemail;
                    cmd.Parameters.Add("@emailsubject", SqlDbType.VarChar).Value = lead_info.emailsubject;
                    cmd.Parameters.Add("@emailbody", SqlDbType.VarChar).Value = lead_info.emailbody;
                    cmd.Parameters.Add("@sendingdatetime", SqlDbType.VarChar).Value = lead_info.sendingdatetime;

                    if (Handle == "Insert")
                        cmd.Parameters.Add("@ISBackend", SqlDbType.Bit).Value = false;
                    else
                        cmd.Parameters.Add("@ISBackend", SqlDbType.Bit).Value = true;


                    con.Open();
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        IsSuccess = "true";
                    }
                    con.Close();

                }
                //
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(logfilepath, Environment.NewLine + ex.ToString());
            }
            return IsSuccess;
        }

        //creates lead in backend and create dnn user 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lead_info"></param>
        /// <param name="DnnRole"></param>
        /// <param name="DnnPortalID"></param>
        /// <returns></returns>
        public bool create_lead(LeadInfo lead_info, string DnnRole, int DnnPortalID, string DnnPortalName)
        {
            string logfilepath = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + portalname + "/MLS_Medias/lead_activity_xml/Shedulers_log_Files/SendMailEx.txt");
            try
            {
                BackendSuccess = 0;
                DotNetNuke.Entities.Users.UserInfo oUser = new DotNetNuke.Entities.Users.UserInfo();
                oUser.PortalID = DnnPortalID;// DotNetNuke.Entities.Portals.PortalController.GetCurrentPortalSettings().PortalId;
                oUser.IsSuperUser = false;
                oUser.FirstName = lead_info.FirstName;
                oUser.LastName = lead_info.LastName;
                oUser.Email = lead_info.EmailID;
                oUser.Username = lead_info.EmailID;
                oUser.DisplayName = lead_info.FirstName + " " + lead_info.LastName;




                //Fill MINIMUM Profile Items (KEY PIECE)
                // oUser.Profile.PreferredLocale = PortalSettings.DefaultLanguage;
                //oUser.Profile.TimeZone = PortalSettings.TimeZoneOffset;

                oUser.Profile.FirstName = oUser.FirstName;
                oUser.Profile.LastName = oUser.LastName;
                oUser.Profile.Telephone = lead_info.PhoneWork;



                //Set Membership
                UserMembership oNewMembership = new UserMembership();
                oNewMembership.Approved = true;
                oNewMembership.CreatedDate = System.DateTime.Now;
                oNewMembership.Email = oUser.Email;
                oNewMembership.IsOnLine = false;
                oNewMembership.Username = oUser.Username;
                oNewMembership.Password = lead_info.DnnPassword;  

                //Bind membership to user
                oUser.Membership = oNewMembership;

                //Add the user, ensure it was successful
                if (UserCreateStatus.Success == UserController.CreateUser(ref oUser))
                {
                    DotNetNuke.Security.Roles.RoleController rc = new DotNetNuke.Security.Roles.RoleController();
                    //retrieve role
                    string groupName = DnnRole;
                    DotNetNuke.Security.Roles.RoleInfo ri = rc.GetRoleByName(DnnPortalID, groupName);
                    //suppose your userinfo object is ui
                    DotNetNuke.Entities.Users.UserInfo ui = DotNetNuke.Entities.Users.UserController.GetCurrentUserInfo();
                    DateTime d = Null.NullDate;
                    rc.AddUserRole(DnnPortalID, Convert.ToInt32(oUser.UserID.ToString()), ri.RoleID, d);
                    lead_info.DnnLeadID = oUser.UserID.ToString();
                    //New Implementation for Saving lead data if Backend is unavailable 
                    InsertLeadDetails(lead_info, "Insert");
                    create_lead(lead_info);
                    SendEmail(lead_info, oUser);
                    createleadXML(DnnPortalID, DnnPortalName, lead_info.DnnLeadID); 
                    return true;  
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                lead_info.DnnLeadID = null;
                System.IO.File.AppendAllText(logfilepath, Environment.NewLine + ex.ToString());
                throw ex;
            }
            return false;
        }

        // convert password in MD5
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TextString"></param>
        /// <returns></returns>
        static private string getMD5(string TextString)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBuffer = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] outputBuffer = md5.ComputeHash(inputBuffer);

            StringBuilder Builder = new StringBuilder(outputBuffer.Length);
            for (int i = 0; i < outputBuffer.Length; i++)
            {
                Builder.Append(outputBuffer[i].ToString("X2"));
            }

            return Builder.ToString();
        }

        //create lead in backend only
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lead_info"></param>
        public void create_lead(LeadInfo lead_info)
        {
            try
            {
                get_connection();

                //create account -----------------------------------------------------------------------------------

                NameValueCollection fieldListCollection = new NameValueCollection();
                //to update a record, you will nee to pass in a record id as commented below

                fieldListCollection.Add("first_name", lead_info.FirstName);
                fieldListCollection.Add("last_name", lead_info.LastName);
                fieldListCollection.Add("phone_mobile", lead_info.PhoneWork);
                fieldListCollection.Add("email1", lead_info.EmailID);
                fieldListCollection.Add("opportunity_amount", lead_info.OpportunityAmount);
                fieldListCollection.Add("lead_source", lead_info.LeadSource);

                if (lead_info.CampaignID != null)   
                    fieldListCollection.Add("campaign_id", lead_info.CampaignID);

                fieldListCollection.Add("description", lead_info.Description);
                fieldListCollection.Add("status", lead_info.Status);
                fieldListCollection.Add("agent_website_c", lead_info.AgentWebsite);

                if (lead_info.CampaignName != null)
                    fieldListCollection.Add("campaign_name_c", lead_info.CampaignName);

                fieldListCollection.Add("company_id_c", lead_info.CompanyID);
                fieldListCollection.Add("agent_type_dom_c", lead_info.AgentID);
                fieldListCollection.Add("password_c", lead_info.DnnPassword);
                fieldListCollection.Add("lender_type_dom_c", lead_info.LenderID);
                fieldListCollection.Add("frontendid_c", lead_info.DnnLeadID);
                fieldListCollection.Add("lead_status_for_lender_c", lead_info.StatusForLender);


                //this is just a trick to avoid having to manually specify index values for name_value[]
                name_value[] fieldList = new name_value[fieldListCollection.Count];

                int count = 0;
                foreach (string name in fieldListCollection)
                {
                    foreach (string value in fieldListCollection.GetValues(name))
                    {
                        name_value field = new name_value();
                        field.name = name;
                        field.value = value;
                        fieldList[count] = field;
                    }
                    count++;
                }

                //creates new leads in backend
                new_set_entry_result result1 = SugarClient.set_entry(SessionID, "Leads", fieldList);
                BackendLeadID = result1.id;
                SugarClient.logout(SessionID);
                //string logfilepath = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/ExitRL/MLS_Medias/lead_activity_xml/Shedulers_log_Files/AddPennyMacData.txt");

                //System.IO.File.AppendAllText(logfilepath, Environment.NewLine + "lead_source:" + lead_info.LeadSource);
                //System.IO.File.AppendAllText(logfilepath, Environment.NewLine + "campaign_name_c:" + lead_info.CampaignName);

                //System.IO.File.AppendAllText(logfilepath, Environment.NewLine + "BackendLeadID:" + BackendLeadID);

                //if (BackendLeadID != null && BackendLeadID != string.Empty)
                //{
                //    string[] penbool = BackendLeadID.Split('_');
                //    if (penbool.Length > 0)
                //        PennyLead = penbool[1];
                //    BackendSuccess = 1;
                //    InsertLeadDetails(lead_info, "Update");
                //}
            }
            catch (Exception ex)
            {
                BackendLeadID = null;
                throw ex;
            }
        }

        //Get particular agent information from backend
        /// <summary>
        /// 
        /// </summary>
        /// <param name="BackendAgentID"></param>
        public DataTable get_agent_info(AgentInfo agent_info)
        {
            DataTable table = null;
            string show_on_cond = "";
            try    
            {
                get_connection();
                var fieldsToRetrieve = new[] { "loanappurl_c", "twitter_url_c", "lender_company_c", "lender_company_logo_c", "linked_in_c", "phone_mobile", "phone_other", "tagline_c", "awards_c", "url_c", "email1", "show_on_site_c", "company_name_c", "users_pic_c", "id", "first_name", "last_name", "address_street", "address_city", "address_state", "address_postalcode", "address_country", "phone_work", "user_name", "description", "title", "facebook_url_c", "agentmls_c", "officemls1_c", "active_rotate_flag_c", "leadsperround_c", "current_rotate_cnt_c", "send_leads_c", "lead_flow_c", "roletype_c", "lender_to_agent_c", "youtube_url_c" };

                if (agent_info.ShowOnSite != null)
                    show_on_cond = " and show_on_site_c='" + agent_info.ShowOnSite + "'";

                string condition = "roletype_c=" + agent_info.RoleType + " and company_name_c='" + agent_info.CompanyID + "' and id_c='" + agent_info.AgentID + "'" + show_on_cond;
                //string condition = "roletype_c=" + "2" + " and company_name_c='" + "23a1d20b-3822-0ca5-b1eb-52257ffdce8a" + "' and id_c='" + "" + "'" + show_on_cond;

                link_name_to_fields_array[] entryList = new link_name_to_fields_array[0];

                var result = SugarClient.get_entry_list(SessionID, "Users", condition, "last_name asc", 0, fieldsToRetrieve, entryList, 250, 0, false);
                table = new DataTable();

                for (int k = 0; k < fieldsToRetrieve.Length; k++)
                    table.Columns.Add(fieldsToRetrieve[k], typeof(string));

                for (int i = 0; i < result.result_count; i++)
                {
                    DataRow dtRow = table.NewRow();
                    for (int j = 0; j < fieldsToRetrieve.Length; j++)
                    {
                        if (fieldsToRetrieve[j].Equals("users_pic_c"))
                        {
                            string users_pic = result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                            if (users_pic != null && users_pic != "")
                                //old Path
                                //dtRow[fieldsToRetrieve[j]] = "http://leads.brokeragent360.com/custom/SynoFieldPhoto/phpThumb/images/" + result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                                //new path
                                dtRow[fieldsToRetrieve[j]] = backimageurl + result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                            else
                                dtRow[fieldsToRetrieve[j]] = "/images/default_users.jpg";
                        }
                        else
                            dtRow[fieldsToRetrieve[j]] = result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                    }
                    table.Rows.Add(dtRow);
                }
                SugarClient.logout(SessionID);
                return table;
            }
            catch (Exception ex)
            {
                throw ex;

            }
            return null;

        }

        //Get particular lead information from backend using front end USERID
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lead_info"></param>
        /// <returns></returns>
        public DataTable get_lead_info(LeadInfo lead_info)
        {
            DataTable table;
            try
            {
                get_connection();


                var fieldsToRetrieve = new[] { "id", "agent_type_dom_c", "lender_type_dom_c", "first_name", "last_name", "title", "phone_home", "phone_mobile", "phone_work", "phone_other", "phone_fax", "primary_address_street", "primary_address_city", "primary_address_state", "primary_address_postalcode", "primary_address_country", "lead_source", "status", "password_c", "frontendid_c", "company_id_c", "lead_status_for_lender_c" };


                link_name_to_fields_array[] entryList = new link_name_to_fields_array[0];

                // var result = SugarClient.get_entry_list(SessionID, "Leads",


                var result = SugarClient.get_entry_list(SessionID, "Leads", "frontendid_c='" + lead_info.DnnLeadID + "' and company_id_c='" + lead_info.CompanyID + "'", "id asc", 0, fieldsToRetrieve, entryList, 1, 0, false);

                table = new DataTable();

                for (int k = 0; k < fieldsToRetrieve.Length; k++)
                    table.Columns.Add(fieldsToRetrieve[k], typeof(string));

                for (int i = 0; i < result.result_count; i++)
                {
                    DataRow dtRow = table.NewRow();
                    for (int j = 0; j < fieldsToRetrieve.Length; j++)
                    {
                        dtRow[fieldsToRetrieve[j]] = result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                    }
                    table.Rows.Add(dtRow);
                }

                SugarClient.logout(SessionID);
                return table;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        #region old_agent_template_SendEmail_28_11_13
        //public void SendEmail(LeadInfo lead_info, UserInfo userinfo)
        //{

        //    try
        //    {


        //        string str_smtpserver = DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPServer");  // DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPServer");
        //        string str_auth = DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPAuthentication");
        //        string str_uname = DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPUsername");
        //        string str_pass = DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPPassword");
        //        string agentPhoto = string.Empty;
        //        String txtcurrentdate = DateTime.Now.ToString();
        //        string str_sub = "New User Registration";

        //        //msg.IsBodyHtml = true;
        //        Uri currentUri = HttpContext.Current.Request.Url;

        //        string tobody = string.Empty; ;

        //        //Code start by ketn
        //        //for send email with agent info to new created lead/user
        //        AgentInfo agent_info = new AgentInfo();

        //        DataTable lead_dt = get_lead_info(lead_info);

        //        DataTable agent_dt = new DataTable();
        //        string logoUrl = string.Empty;
        //        string company_name = string.Empty;
        //        string site_url = string.Empty;
        //        if (lead_dt.Rows.Count > 0)
        //        {

        //            try
        //            {
        //                agent_info.AgentID = lead_dt.Rows[0]["agent_type_dom_c"].ToString();
        //                agent_info.CompanyID = lead_info.CompanyID;
        //                agent_info.RoleType = "2";
        //                agent_info.ShowOnSite = "Active";
        //                //Fetch the agent detail from  backend this function presents in ba360_lib.dll
        //                agent_dt = get_agent_info(agent_info);
        //            }
        //            catch (Exception ex)
        //            {

        //            }
        //        }
        //        if (agent_dt.Rows.Count > 0)
        //        {

        //            logoUrl = "http://" + currentUri.Host + "/Portals/0/exitrealestategallery.png";

        //            if (agent_dt.Rows[0]["users_pic_c"].ToString().Contains("default_users.jpg"))
        //            {
        //                agentPhoto = "http://" + currentUri.Host + "/images/default_users.jpg";

        //            }
        //            else
        //            {
        //                agentPhoto = agent_dt.Rows[0]["users_pic_c"].ToString();
        //            }


        //            //for agent url
        //            if (agent_dt.Rows[0]["url_c"].ToString() != "")
        //            {
        //                if (agent_dt.Rows[0]["url_c"].ToString().Contains(".com"))
        //                {
        //                    if (agent_dt.Rows[0]["url_c"].ToString().Contains("http://"))
        //                    {
        //                        site_url = agent_dt.Rows[0]["url_c"].ToString();
        //                    }
        //                    else
        //                    {
        //                        site_url = "http://" + agent_dt.Rows[0]["url_c"].ToString();
        //                    }
        //                }
        //                else { site_url = currentUri.Host.Replace("www.", ""); }
        //            }
        //            else
        //            {
        //                site_url = currentUri.Host.Replace("www.", "");
        //            }
        //            /////
        //            tobody = "<div style='width:92%;'>";
        //            tobody += "<table><tr><td>";
        //            tobody += "<br/>Hello " + lead_info.FirstName + " " + lead_info.LastName + ",";
        //            tobody += "<br/><br/>";
        //            tobody += "Welcome and thank you for registering on our FREE membership site. You now have the ability to easily search all available properties in your area, the same way we REALTORS do!";
        //            tobody += "<br/><br/>";
        //            tobody += "To activate your new FREE membership account please <a target='_blank' href='" + site_url + "/Lead_Dashboard.aspx?T=S&uid=" + userinfo.UserID + "'>click here.</a>";
        //            tobody += "<br/><br/>";
        //            tobody += "Username:&nbsp;" + lead_info.EmailID + "<br/>" + "Password:&nbsp;" + lead_info.DnnPassword;
        //            tobody += "<br/><br/>";
        //            tobody += "<span>•	You now have full access to your local Multiple Listing Service, making your search for the perfect home a snap.</span><br />";
        //            tobody += "<span>•   You may also save searches so that you can refer back to them at any time and receive up to date information on current activity in your desired area(s).</span><br />";
        //            //tobody += "<span>&nbsp;desired area(s).</span><br />";
        //            tobody += "<span>•	All of this in the privacy of your own home at your convenience.</span>";
        //            tobody += "<br/><br/>";
        //            tobody += "<span>When you come across a property you like and want more information, simply pick up the phone and call me. Should you be pressed for time and can’t call me&nbsp;</span>";
        //            tobody += "<span>at that moment, just save the property as a favorite and you can call me at your convenience. Remember, there is no such thing as a silly question.&nbsp;</span>";
        //            tobody += "<span>I am here to help make your home buying experience as pleasant and stress free as possible.</span>";
        //            tobody += "</td></tr>";
        //            tobody += "</table>";

        //            tobody += "<div style='width:100%;'>";
        //            tobody += "<table cellspacing='0' cellpadding='10' border='1' style='border: solid #c2c2c2 1px;background: #ECECEC;box-shadow: 0px 10px 5px -6px #777;padding: 15px 18px 18px;'>";
        //            tobody += "<tr>";
        //            tobody += "<td style='border: medium none;'>";
        //            tobody += "<table cellspacing='5' cellpadding='0' border='0'>";
        //            tobody += "<tr>";
        //            tobody += "<td>";
        //            tobody += "<table style='border: 1px solid #777; padding: 7px;'  cellspacing='0' cellpadding='2' >";
        //            tobody += "<td width='3%' valign='top' >";
        //            tobody += "<table><tr><td><p>";
        //            tobody += "<img width='100' height='auto' align='left'  style='background: #fff; border: 3px solid #FFFFFF; border-radius: 5px;' src='" + agentPhoto + "'>";
        //            tobody += "</p>";
        //            tobody += "</td>";
        //            tobody += "</tr>";
        //            tobody += "<tr>";
        //            tobody += "<td style='padding: 4px;' colspan='3'>";
        //            tobody += " <p align='right' style='text-align: right;'>";
        //            tobody += "<span style='font-family: verdana; font-size: 12px;'>";
        //            tobody += "<a target='_blank' href='mailto:" + agent_dt.Rows[0]["email1"].ToString() + "'>";
        //            tobody += "<span style='text-decoration: none;'>";
        //            tobody += "<img border='0' alt='Send an Email' src='" + site_url + "/Portals/" + "ExitRL" + "/MLS_Medias/Property/SendMail.png' >";
        //            tobody += "</span>";
        //            tobody += "</a>";
        //            tobody += "</span>";
        //            tobody += "</p>";
        //            tobody += "</td>";
        //            tobody += "</tr>";
        //            tobody += "</table>";
        //            tobody += "</td>";
        //            tobody += "<td width='60%' valign='top' style='vertical-align: top; padding-left: 10px;'>";
        //            tobody += "<div>";
        //            if (agent_dt.Rows[0]["title"].ToString() != "")
        //            {
        //                tobody += "<b><span style='font-family: verdana; color: rgb(0, 120, 144); text-shadow: 0px 1px 1px rgb(196, 214, 234);font-size: 16px;'>" + agent_dt.Rows[0]["first_name"].ToString() + " " + agent_dt.Rows[0]["last_name"].ToString() + "," + agent_dt.Rows[0]["title"].ToString() + "</span></b>";
        //            }
        //            else
        //            {
        //                tobody += "<b><span style='font-family: verdana; color: rgb(0, 120, 144); text-shadow: 0px 1px 1px rgb(196, 214, 234);font-size: 16px;'>" + agent_dt.Rows[0]["first_name"].ToString() + " " + agent_dt.Rows[0]["last_name"].ToString() + "</span></b>";
        //            }

        //            tobody += "<span style='font-family: verdana; font-size: 12px;'><br>";
        //            tobody += " <br>";
        //            tobody += "<b>" + System.Web.Configuration.WebConfigurationManager.AppSettings["companyname"] + "</b><br>";
        //            tobody += "<br>";
        //            tobody += "<b>Cell Phone: </b>" + agent_dt.Rows[0]["phone_work"].ToString();
        //            tobody += "<br>";
        //            tobody += "<br>";
        //            tobody += "<b>Email:</b> <a href='mailto:" + agent_dt.Rows[0]["email1"].ToString() + "' target='_blank'><span style='color: #0a88a1;'>" + agent_dt.Rows[0]["email1"].ToString() + "</span></a><br>";
        //            tobody += " <br>";
        //            if (agent_dt.Rows[0]["url_c"].ToString() != "")
        //            {
        //                if (agent_dt.Rows[0]["url_c"].ToString().Contains(".com"))
        //                {
        //                    if (agent_dt.Rows[0]["url_c"].ToString().Contains("http://"))
        //                    {
        //                        //tobody += "<b>Website: </b><a href='" + agent_dt.Rows[0]["url_c"].ToString() + "/Home.aspx?uid=" + lead_id + "&lastopen=" + lastmailopenfor + "' style='color: #0a88a1;'  target='_blank'><span style='color: #0a88a1;'>" + agent_dt.Rows[0]["url_c"].ToString() + "</span></a></span>";
        //                        tobody += "<b>Website: </b><a href='" + agent_dt.Rows[0]["url_c"].ToString() + "/Home.aspx?uid=" + lead_info.DnnLeadID + "' style='color: #0a88a1;'  target='_blank'><span style='color: #0a88a1;'>" + agent_dt.Rows[0]["url_c"].ToString() + "</span></a></span>";
        //                    }
        //                    else
        //                    {
        //                        tobody += "<b>Website: </b><a href='http://" + agent_dt.Rows[0]["url_c"].ToString() + "/Home.aspx?uid=" + lead_info.DnnLeadID + "' style='color: #0a88a1;'  target='_blank'><span style='color: #0a88a1;'>" + agent_dt.Rows[0]["url_c"].ToString() + "</span></a></span>";

        //                    }
        //                }
        //            }
        //            tobody += "</div>";
        //            tobody += "</td>";
        //            tobody += "</table>";
        //            tobody += "</td>";
        //            tobody += "<td width='10%' valign='top' style='width: 10.0%; padding: 0cm 0cm 0cm 0cm;'>";
        //            tobody += "<table>";
        //            tobody += " <tr>";
        //            tobody += "<td>";
        //            tobody += "<p>";
        //            tobody += "<img width='149' height='auto' align='right' src='" + site_url + "/Portals/0/exitrealestategallery.png' >";
        //            tobody += "</p>";
        //            tobody += "</td>";
        //            tobody += "</tr>";
        //            tobody += "</table>";
        //            tobody += "</td>";
        //            tobody += "</tr>";
        //            tobody += "</table>";
        //            tobody += "</td>";
        //            tobody += "</tr>";
        //            tobody += "</table>";
        //            tobody += "</div>";
        //            tobody += "<div><a target='_blank' href='" + site_url + "/Home.aspx?optout=optout&lid=" + lead_info.DnnLeadID + "'>Click here to opt out</a></div>";
        //            string rVal1 = DotNetNuke.Services.Mail.Mail.SendMail("info@" + currentUri.Host.Replace("www.", ""), lead_info.EmailID, "", "", DotNetNuke.Services.Mail.MailPriority.Normal, str_sub, DotNetNuke.Services.Mail.MailFormat.Html, System.Text.Encoding.UTF8, tobody, "", str_smtpserver, str_auth, str_uname, str_pass);

        //            ////Code End by ketn
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //}
        #endregion

        public void SendEmail(LeadInfo lead_info, UserInfo userinfo)
        {
           
            try
            {
               // string portalname = "ExitRL";
                string SiteLogo = string.Empty, agentname = string.Empty, AgentWebsite = string.Empty, AgentWebsiteText = string.Empty, agentTitle = string.Empty;

                //Depricated in new dnn
                //string str_smtpserver = DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPServer");  
                //string str_auth = DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPAuthentication");
                //string str_uname = DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPUsername");
                //string str_pass = DotNetNuke.Entities.Host.HostSettings.GetHostSetting("SMTPPassword");

                string str_smtpserver = DotNetNuke.Entities.Host.Host.SMTPServer;
                string str_auth = DotNetNuke.Entities.Host.Host.SMTPAuthentication;
                string str_uname = DotNetNuke.Entities.Host.Host.SMTPUsername;
                string str_pass = DotNetNuke.Entities.Host.Host.SMTPPassword;
                bool smtpenable = DotNetNuke.Entities.Host.Host.EnableSMTPSSL;

                string agentPhoto = string.Empty;
                String txtcurrentdate = DateTime.Now.ToString();
                string str_sub = "New User Registration";
                DataTable lead_dt = new DataTable();

                //msg.IsBodyHtml = true;
                Uri currentUri = HttpContext.Current.Request.Url;

                string tobody = string.Empty;
                StringBuilder EmailText = new StringBuilder();
                //Code start by ketn
                //for send email with agent info to new created lead/user
                AgentInfo agent_info = new AgentInfo();

                try
                {
                    lead_dt = get_lead_info(lead_info);
                }
                catch (Exception ex)
                {
                    throw ex;

                }

                DataTable agent_dt = new DataTable();
                string logoUrl = string.Empty;
                string company_name = string.Empty;
                string site_url = string.Empty;

                if (lead_dt.Rows.Count > 0)
                {
                    try
                    {
                        agent_info.AgentID = lead_dt.Rows[0]["agent_type_dom_c"].ToString();
                        agent_info.CompanyID = lead_info.CompanyID;
                        agent_info.RoleType = "2";

                        //comment for send welcome email even assign agent of lead is active or not
                        //agent_info.ShowOnSite = "Active";

                        //Fetch the agent detail from  backend this function presents in ba360_lib.dll
                        agent_dt = get_agent_info(agent_info);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                if (agent_dt.Rows.Count > 0)
                {
                    logoUrl = "http://" + currentUri.Host + "/Portals/0/exitrealestategallery.png";

                    if (agent_dt.Rows[0]["users_pic_c"].ToString().Contains("default_users.jpg"))
                    {
                        agentPhoto = "http://" + currentUri.Host + "/images/default_users.jpg";

                    }
                    else
                    {
                        agentPhoto = agent_dt.Rows[0]["users_pic_c"].ToString();
                    }

                    //for agent url
                    if (agent_dt.Rows[0]["url_c"].ToString() != "")
                    {
                        //if (agent_dt.Rows[0]["url_c"].ToString().Contains(".com"))
                        //{
                        if (agent_dt.Rows[0]["url_c"].ToString().Contains("http://"))
                        {
                            site_url = agent_dt.Rows[0]["url_c"].ToString();
                            AgentWebsite = agent_dt.Rows[0]["url_c"].ToString() + "/Home.aspx?uid=" + lead_info.DnnLeadID;
                            AgentWebsiteText = agent_dt.Rows[0]["url_c"].ToString();
                        }
                        else
                        {
                            site_url = "http://" + agent_dt.Rows[0]["url_c"].ToString();
                            AgentWebsite = "http://" + agent_dt.Rows[0]["url_c"].ToString() + "/Home.aspx?uid=" + lead_info.DnnLeadID;
                            AgentWebsiteText = agent_dt.Rows[0]["url_c"].ToString();
                        }
                        //}
                        //else { site_url = currentUri.Host.Replace("www.", ""); }
                    }
                    else
                    {
                        site_url = currentUri.Host.Replace("www.", "");
                    }

                    /////
                    EmailText.Append("<div>");
                    EmailText.Append("<div style='font-weight: bold; font-size: 20px;'>Hello " + lead_info.FirstName + " " + lead_info.LastName + ", </div>");
                    EmailText.Append("<br/><br/>");
                    EmailText.Append("Welcome and thank you for registering on our FREE membership site. You now have the ability to easily search all available properties in your area, the same way we REALTORS do!");
                    EmailText.Append("<br/><br/>");
                    EmailText.Append("To activate your new FREE membership account please <a target='_blank' href='" + site_url + "/Lead_Dashboard.aspx?T=S&uid=" + userinfo.UserID + "'>click here.</a>");
                    EmailText.Append("<br/><br/>");
                    EmailText.Append("Username:&nbsp;" + lead_info.EmailID + "<br/>" + "Password:&nbsp;" + lead_info.DnnPassword);
                    EmailText.Append("<br/><br/>");
                    EmailText.Append("<span>•	You now have full access to your local Multiple Listing Service, making your search for the perfect home a snap.</span><br />");
                    EmailText.Append("<span>•   You may also save searches so that you can refer back to them at any time and receive up to date information on current activity in your desired area(s).</span><br />");
                    //tobody += "<span>&nbsp;desired area(s).</span><br />";
                    EmailText.Append("<span>•	All of this in the privacy of your own home at your convenience.</span>");
                    EmailText.Append("<br/><br/>");
                    EmailText.Append("<span>When you come across a property you like and want more information, simply pick up the phone and call me. Should you be pressed for time and can’t call me&nbsp;</span>");
                    EmailText.Append("<span>at that moment, just save the property as a favorite and you can call me at your convenience. Remember, there is no such thing as a silly question.&nbsp;</span>");
                    EmailText.Append("<span>I am here to help make your home buying experience as pleasant and stress free as possible.</span>");
                    EmailText.Append("<div><a target='_blank' href='" + site_url + "/Home.aspx?optout=optout&lid=" + lead_info.DnnLeadID + "'>Click here to opt out</a></div>");
                    EmailText.Append("</div>");

                    SiteLogo = site_url + "/" + portalCntrlr.GetPortal(0).HomeDirectory + "/" + portalCntrlr.GetPortal(0).LogoFile;

                    agentname = agent_dt.Rows[0]["first_name"].ToString() + " " + agent_dt.Rows[0]["last_name"].ToString();

                    agentTitle = agent_dt.Rows[0]["title"].ToString();

                    tobody += AgentTemplate(agentPhoto, agentname, agentTitle, agent_dt.Rows[0]["phone_mobile"].ToString(), agent_dt.Rows[0]["email1"].ToString(), AgentWebsite, AgentWebsiteText, SiteLogo, DateTime.Now.ToString("MMMM d, yyyy"), Convert.ToString(EmailText), portalname);
                    ///////Agent Template end


                    List<To> to = new List<To>();
                    to.Add(new To { address = lead_info.EmailID });

                    string rVal1 = new Mail.Mail().Sendmail(str_uname, "", to, new List<CC>(), new List<BCC>(), new List<replyTo>(), tobody, str_sub, new List<Attachment>(), MailPriority.Normal, true, Encoding.UTF8, str_smtpserver, str_uname, str_pass, smtpenable);


                    //string rVal1 = DotNetNuke.Services.Mail.Mail.SendMail(str_uname, lead_info.EmailID, "", "", DotNetNuke.Services.Mail.MailPriority.Normal, str_sub, DotNetNuke.Services.Mail.MailFormat.Html, System.Text.Encoding.UTF8, tobody, "", str_smtpserver, str_auth, str_uname, str_pass);


                    //OLD USE   
                    //string rVal1 = DotNetNuke.Services.Mail.Mail.SendMail("info@" + currentUri.Host.Replace("www.", ""),lead_info.EmailID, "", "", DotNetNuke.Services.Mail.MailPriority.Normal, str_sub, DotNetNuke.Services.Mail.MailFormat.Html, System.Text.Encoding.UTF8, tobody, "", str_smtpserver, str_auth, str_uname, str_pass);

                    ////Code End by ketan 

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        //create xml for leads to store leads history & activities
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DnnPortalID"></param>
        /// <param name="DnnPortalName"></param>
        /// <param name="DnnLeadID"></param>
        public void createleadXML(int DnnPortalID, string DnnPortalName, string DnnLeadID)
        {
            string filepath = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + DnnPortalName + "/MLS_Medias/lead_activity_xml/" + DnnLeadID + ".xml");

            if (!Directory.Exists(Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + DnnPortalName + "/MLS_Medias")))// + "/MLS_Medias/Property/" + PropertyID + "/"+ObjectType)))
            {
                // Specify a "currently active folder" 
                string activeDir = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + DnnPortalName + "/");

                //Create a new subfolder under the current active folder 
                string newPath = System.IO.Path.Combine(activeDir, "MLS_Medias");

                // Create the subfolder
                if (!Directory.Exists(newPath))
                    System.IO.Directory.CreateDirectory(newPath);
            }

            if (!Directory.Exists(Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + DnnPortalName + "/MLS_Medias/lead_activity_xml")))// + "/MLS_Medias/Property/" + PropertyID + "/"+ObjectType)))
            {
                // Specify a "currently active folder" 
                string activeDir = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + DnnPortalName + "/MLS_Medias/");

                //Create a new subfolder under the current active folder 
                string newPath = System.IO.Path.Combine(activeDir, "lead_activity_xml");

                // Create the subfolder
                if (!Directory.Exists(newPath))
                    System.IO.Directory.CreateDirectory(newPath);
            }

            XmlTextWriter xtw = new XmlTextWriter((filepath), System.Text.Encoding.UTF8);
            xtw.Formatting = System.Xml.Formatting.Indented;
            xtw.Indentation = 3;
            xtw.IndentChar = ' ';
            xtw.WriteStartDocument(true);
            xtw.WriteStartElement("Activities");
            xtw.WriteEndElement();
            xtw.WriteEndDocument();
            xtw.Close();

        }

        //insert record to traffic dashboard module
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lead_info"></param>
        public void set_traffic_data(LeadInfo lead_info)
        {


            try
            {

                get_connection();

                if (SessionID != String.Empty)
                {
                    NameValueCollection fieldListCollection = new NameValueCollection();
                    //to update a record, you will nee to pass in a record id as commented below

                    fieldListCollection.Add("name", lead_info.LeadSource);
                    fieldListCollection.Add("campaign_name_c", lead_info.CampaignName);
                    fieldListCollection.Add("form_views_c", lead_info.form_views_c);
                    fieldListCollection.Add("search_count_c", lead_info.search_count_c);
                    fieldListCollection.Add("company_id_c", lead_info.CompanyID);

                    //this is just a trick to avoid having to manually specify index values for name_value[]
                    name_value[] fieldList = new name_value[fieldListCollection.Count];

                    int count = 0;
                    foreach (string name in fieldListCollection)
                    {
                        foreach (string value in fieldListCollection.GetValues(name))
                        {
                            name_value field = new name_value();
                            field.name = name; field.value = value;
                            fieldList[count] = field;
                        }
                        count++;
                    }
                    new_set_entry_result result1 = SugarClient.set_entry(SessionID, "tbl_Traffic_Dashboard", fieldList);
                    RecordID = result1.id;
                    //string RecordID = result1.id;
                    SugarClient.logout(SessionID);


                }
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        //update traffic data using id
        public void update_traffic_data(string trafficId)
        {


            try
            {

                get_connection();

                if (SessionID != String.Empty)
                {
                    NameValueCollection fieldListCollection = new NameValueCollection();
                    //to update a record, you will nee to pass in a record id as commented below


                    fieldListCollection.Add("search_count_c", "1");
                    fieldListCollection.Add("id", trafficId);

                    //this is just a trick to avoid having to manually specify index values for name_value[]
                    name_value[] fieldList = new name_value[fieldListCollection.Count];

                    int count = 0;
                    foreach (string name in fieldListCollection)
                    {
                        foreach (string value in fieldListCollection.GetValues(name))
                        {
                            name_value field = new name_value();
                            field.name = name; field.value = value;
                            fieldList[count] = field;
                        }
                        count++;
                    }
                    new_set_entry_result result1 = SugarClient.set_entry(SessionID, "tbl_Traffic_Dashboard", fieldList);

                    SugarClient.logout(SessionID);


                }
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        //update traffic data using id for fromview
        public void update_traffic_data_formview(string trafficId)
        {


            try
            {

                get_connection();

                if (SessionID != String.Empty)
                {
                    NameValueCollection fieldListCollection = new NameValueCollection();
                    //to update a record, you will nee to pass in a record id as commented below


                    fieldListCollection.Add("form_views_c", "1");
                    fieldListCollection.Add("id", trafficId);

                    //this is just a trick to avoid having to manually specify index values for name_value[]
                    name_value[] fieldList = new name_value[fieldListCollection.Count];

                    int count = 0;
                    foreach (string name in fieldListCollection)
                    {
                        foreach (string value in fieldListCollection.GetValues(name))
                        {
                            name_value field = new name_value();
                            field.name = name; field.value = value;
                            fieldList[count] = field;
                        }
                        count++;
                    }
                    new_set_entry_result result1 = SugarClient.set_entry(SessionID, "tbl_Traffic_Dashboard", fieldList);

                    SugarClient.logout(SessionID);


                }
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        //insert record to Meeting module
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lead_info"></param>
        public void set_meeting_data(MeetingInfo meeting_info)
        {

            try
            {

                get_connection();

                if (SessionID != String.Empty)
                {
                    NameValueCollection fieldListCollection = new NameValueCollection();
                    //to update a record, you will nee to pass in a record id as commented below

                    fieldListCollection.Add("name", meeting_info.Name);
                    fieldListCollection.Add("description", meeting_info.Description);
                    fieldListCollection.Add("assigned_user_id", meeting_info.AssignAgentID);
                    fieldListCollection.Add("location", meeting_info.Location);
                    fieldListCollection.Add("duration_hours", meeting_info.DurationHours);
                    fieldListCollection.Add("duration_minutes", meeting_info.DurationMinutes);
                    fieldListCollection.Add("date_start", meeting_info.DateStart);
                    //fieldListCollection.Add("date_end", meeting_info.DateEnd);
                    fieldListCollection.Add("parent_type", meeting_info.ParentType);
                    fieldListCollection.Add("status", meeting_info.Status);
                    fieldListCollection.Add("type", meeting_info.Type);
                    fieldListCollection.Add("parent_id", meeting_info.LeadID);
                    fieldListCollection.Add("reminder_time", meeting_info.ReminderTime);
                    fieldListCollection.Add("email_reminder_time", meeting_info.EmailReminderTime);
                    fieldListCollection.Add("company_id_c", meeting_info.CompanyID);
                    fieldListCollection.Add("email_reminder_sent", "0");
                    fieldListCollection.Add("user_invitees", meeting_info.AssignAgentID);

                    //this is just a trick to avoid having to manually specify index values for name_value[]
                    name_value[] fieldList = new name_value[fieldListCollection.Count];

                    int count = 0;
                    foreach (string name in fieldListCollection)
                    {
                        foreach (string value in fieldListCollection.GetValues(name))
                        {
                            name_value field = new name_value();
                            field.name = name; field.value = value;
                            fieldList[count] = field;
                        }
                        count++;
                    }
                    new_set_entry_result result1 = SugarClient.set_entry(SessionID, "Meetings", fieldList);
                    string RecordID = result1.id;
                    SugarClient.logout(SessionID);


                }
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        //insert record to Web_web module
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lead_info"></param>  
        public void set_web_activity_data(LeadInfo lead_info)
        {
            string RecordID = string.Empty;
            try
            {

                get_connection();

                if (SessionID != String.Empty)
                {
                    NameValueCollection fieldListCollection = new NameValueCollection();
                    //to update a record, you will nee to pass in a record id as commented below

                    fieldListCollection.Add("created_by_lead_c", lead_info.CreatedBY);
                    fieldListCollection.Add("description", lead_info.Description);
                    fieldListCollection.Add("comment_c", lead_info.Comment);
                    fieldListCollection.Add("activity_type_c", lead_info.ActivityType);
                    fieldListCollection.Add("name", lead_info.Name);


                    //this is just a trick to avoid having to manually specify index values for name_value[]
                    name_value[] fieldList = new name_value[fieldListCollection.Count];

                    int count = 0;
                    foreach (string name in fieldListCollection)
                    {
                        foreach (string value in fieldListCollection.GetValues(name))
                        {
                            name_value field = new name_value();
                            field.name = name; field.value = value;
                            fieldList[count] = field;
                        }
                        count++;
                    }
                    new_set_entry_result result1 = SugarClient.set_entry(SessionID, "web_web_activity", fieldList);
                    RecordID = result1.id;
                    // RecordID = result1.ToString();
                    SugarClient.logout(SessionID);


                }
            }
            catch (Exception ex)
            {
                throw ex; //new Exception(RecordID.ToString());   

            }
        }

        /// <summary>
        /// Set lead OptOut or OptIn in frontend sites.
        /// to set OptOut lead pass the boolean true vale for parameter isOptOut and for OptIn lead pass false boolean value for isOptOut.
        /// </summary>
        /// <param name="leadID">pass UserID </param>
        /// <param name="portalId"></param>
        /// <param name="PortalName"></param>
        /// <param name="isOptOut"></param>
        /// <returns></returns>
        public string set_OptOutInvalidEmail(int leadID, int portalId, string PortalName, bool isOptOut,string NewEmail)
        {
            
            string CompanyID = System.Web.Configuration.WebConfigurationManager.AppSettings["companyID"];
            UserInfo lead = DotNetNuke.Entities.Users.UserController.GetUserById(0, leadID);

            LeadInfo lead_info = new LeadInfo();

            var loginStatus = UserLoginStatus.LOGIN_FAILURE;
            string isoptout = "false";
            if (lead != null)
            {
                /*
                UserMembership ums = new UserMembership();
                ums.Approved = isOptOut ? false : true;
                ums.IsOnLine = isOptOut ? false : true;
                lead.Membership = ums;*/


                lead_info.DnnLeadID = lead.UserID.ToString();
                lead_info.CompanyID = CompanyID;
                lead_info.FirstName = lead.FirstName;
                lead_info.LastName = lead.LastName;
                lead_info.EmailID = NewEmail;

                string Password = DotNetNuke.Entities.Users.UserController.GetPassword(ref lead, lead.Membership.PasswordAnswer);

                lead_info.DnnPassword = Password;

                //rc.DeleteUserRole(portalId, leadID, LeadRoleInfo.RoleID);
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    string xmlpath = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + PortalName + "/MLS_Medias/lead_activity_xml/");
                    xmlDoc.Load(xmlpath + leadID + ".xml");

                    if (isOptOut == false)
                    {
                        ChangeRoleofUser(portalId, leadID, lead, "Lead", "OptedOutLead");
                        SetLeadOptOut_OptIn_InBackend_InvalidEmail(lead.Email, NewEmail, isOptOut);
                        ChangeInvalid_Email_Backend(lead.Email, NewEmail, leadID.ToString(), CompanyID);
                        SendEmail(lead_info, lead);
                        XmlNode InvalidEmailtag = xmlDoc.SelectSingleNode("/Activities/Invalid_Email");
                        if (InvalidEmailtag != null)
                        {
                            InvalidEmailtag.InnerText = "false";
                        }
                        xmlDoc.DocumentElement.AppendChild(InvalidEmailtag);
                    }
                    else
                    {
                        ChangeRoleofUser(portalId, leadID, lead, "OptedOutLead", "Lead");
                    }



                   

                        
                        XmlNode optedoutNode = xmlDoc.SelectSingleNode("/Activities/isOptedOut");

                        if (optedoutNode == null)
                        {
                            XmlElement OptedOutElement = xmlDoc.CreateElement("isOptedOut");
                            XmlText OptedOutText;
                            if (isOptOut)
                                OptedOutText = xmlDoc.CreateTextNode("1");
                            else
                                OptedOutText = xmlDoc.CreateTextNode("0");

                            OptedOutElement.AppendChild(OptedOutText);
                            xmlDoc.DocumentElement.AppendChild(OptedOutElement);
                        }
                        else
                        {
                            if (isOptOut)
                                optedoutNode.InnerText = "1";
                            else
                                optedoutNode.InnerText = "0";
                            xmlDoc.DocumentElement.AppendChild(optedoutNode);

                        }
                        xmlDoc.Save(xmlpath + leadID + ".xml");
                        isoptout = "true";
                    
                }
                catch (Exception ex)
                {
                    isoptout = ex.Message.ToString();
                }
                //string qury = "update [UserPortals] set Authorised=0 where UserId=" + userid;
                //DotNetNuke.Data.DataProvider.Instance().ExecuteNonQuery("prc_property_search", qury);
            }
            return isoptout;
        }

        public void ChangeInvalid_Email_Backend(string OldEmail, string NewEmail, string UserID, string CompanyID)
        {

            
            get_connection();
            Updateresults result = SugarClient.Update_emailLead(SessionID, OldEmail, NewEmail, UserID, CompanyID);

        }

        public string set_OptOut(int leadID, int portalId, string PortalName, bool isOptOut)
        {
            UserInfo lead_info = DotNetNuke.Entities.Users.UserController.GetUserById(0, leadID);
            var loginStatus = UserLoginStatus.LOGIN_FAILURE;
            string isoptout = "false";
            if (lead_info != null)
            {

                UserMembership ums = new UserMembership();
                ums.Approved = isOptOut ? false : true;
                ums.IsOnLine = isOptOut ? false : true;
                lead_info.Membership = ums;

                try
                {
                    ProfileController.UpdateUserProfile(lead_info);
                    UserController.UpdateUser(portalId, lead_info);

                    SetLeadOptOut_OptIn_InBackend(lead_info.Email, isOptOut);

                    XmlDocument xmlDoc = new XmlDocument();
                    string xmlpath = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + PortalName + "/MLS_Medias/lead_activity_xml/");
                    xmlDoc.Load(xmlpath + leadID + ".xml");
                    XmlNode optedoutNode = xmlDoc.SelectSingleNode("/Activities/isOptedOut");
                    if (!isOptOut)
                        ChangeRoleofUser(portalId, leadID, lead_info, "Lead", "OptedOutLead");

                    if (optedoutNode == null)
                    {
                        XmlElement OptedOutElement = xmlDoc.CreateElement("isOptedOut");
                        XmlText OptedOutText;
                        if (isOptOut)
                            OptedOutText = xmlDoc.CreateTextNode("1");
                        else
                            OptedOutText = xmlDoc.CreateTextNode("0");

                        OptedOutElement.AppendChild(OptedOutText);
                        xmlDoc.DocumentElement.AppendChild(OptedOutElement);
                    }
                    else
                    {
                        if (isOptOut)
                            optedoutNode.InnerText = "1";
                        else
                            optedoutNode.InnerText = "0";
                        xmlDoc.DocumentElement.AppendChild(optedoutNode);

                    }
                    xmlDoc.Save(xmlpath + leadID + ".xml");
                    isoptout = "true";
                }
                catch (Exception ex)
                {
                    isoptout = ex.Message.ToString();
                }
                //string qury = "update [UserPortals] set Authorised=0 where UserId=" + userid;
                //DotNetNuke.Data.DataProvider.Instance().ExecuteNonQuery("prc_property_search", qury);
            }
            return isoptout;
        }

        public bool ChangeRoleofUser(int portalId, int leadID, UserInfo objUser, string NewRole, string OldRole)
        {
            try
            {
                DotNetNuke.Security.Roles.RoleController rc = new DotNetNuke.Security.Roles.RoleController();
                DotNetNuke.Security.Roles.RoleInfo OptoutID = rc.GetRoleByName(portalId, NewRole);
                DateTime d = Null.NullDate;
                rc.AddUserRole(portalId, leadID, OptoutID.RoleID, d);
                DotNetNuke.Security.Roles.RoleInfo LeadRoleInfo = rc.GetRoleByName(portalId, OldRole);
                PortalSettings portalinfo = PortalController.GetCurrentPortalSettings();
                rc.DeleteUserRole(portalId, leadID, LeadRoleInfo.RoleID);
                //DotNetNuke.Security.Roles.RoleController.DeleteUserRole(objUser, LeadRoleInfo, portalinfo, false);
                return true;
            }
            catch (Exception ee)
            {
                return false;
            }
        }

        public void SetLeadOptOut_OptIn_InBackend_InvalidEmail(string UserEmail,string NewEmail, bool setOptOutoptIn)
        {
            try
            {
                string backendEmailRecordID = get_email_detail(UserEmail);
                if (backendEmailRecordID != string.Empty)
                {
                    get_connection();

                    //create account -----------------------------------------------------------------------------------

                    NameValueCollection fieldListCollection = new NameValueCollection();
                    //to update a record, you will nee to pass in a record id as commented below

                    fieldListCollection.Add("id", backendEmailRecordID);

                    if (setOptOutoptIn)
                    {
                        fieldListCollection.Add("opt_out", "1");
                    }
                    else
                    {
                        fieldListCollection.Add("opt_out", "0");
                    }


                    //this is just a trick to avoid having to manually specify index values for name_value[]
                    name_value[] fieldList = new name_value[fieldListCollection.Count];

                    int count = 0;
                    foreach (string name in fieldListCollection)
                    {
                        foreach (string value in fieldListCollection.GetValues(name))
                        {
                            name_value field = new name_value();
                            field.name = name;
                            field.value = value;
                            fieldList[count] = field;
                        }
                        count++;
                    }

                    //creates new leads in backend
                    new_set_entry_result result1 = SugarClient.set_entry(SessionID, "EmailAddresses", fieldList);


                    SugarClient.logout(SessionID);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// Set lead OptOut or OptIn in backend site .
        /// to set OptOut lead pass the boolean true vale for parameter setOptOutoptIn and for OptIn lead pass false boolean value for setOptOutoptIn.
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <param name="setOptOutoptIn"></param>
        public void SetLeadOptOut_OptIn_InBackend(string UserEmail, bool setOptOutoptIn)
        {
            try
            {
                string backendEmailRecordID = get_email_detail(UserEmail);
                if (backendEmailRecordID != string.Empty)
                {
                    get_connection();

                    //create account -----------------------------------------------------------------------------------

                    NameValueCollection fieldListCollection = new NameValueCollection();
                    //to update a record, you will nee to pass in a record id as commented below

                    fieldListCollection.Add("id", backendEmailRecordID);

                    if (setOptOutoptIn)
                    {
                        fieldListCollection.Add("opt_out", "1");
                    }
                    else
                    {
                        fieldListCollection.Add("opt_out", "0");
                    }


                    //this is just a trick to avoid having to manually specify index values for name_value[]
                    name_value[] fieldList = new name_value[fieldListCollection.Count];

                    int count = 0;
                    foreach (string name in fieldListCollection)
                    {
                        foreach (string value in fieldListCollection.GetValues(name))
                        {
                            name_value field = new name_value();
                            field.name = name;
                            field.value = value;
                            fieldList[count] = field;
                        }
                        count++;
                    }

                    //creates new leads in backend
                    new_set_entry_result result1 = SugarClient.set_entry(SessionID, "EmailAddresses", fieldList);
                   

                    SugarClient.logout(SessionID);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string get_email_detail(string UserEmail)
        {
            DataTable table = new DataTable();

            try
            {
                get_connection();
                var fieldsToRetrieve = new[] { "id" };

                string condition = "email_address='" + UserEmail + "' and email_address_caps='" + UserEmail.ToUpper() + "'";

                link_name_to_fields_array[] entryList = new link_name_to_fields_array[0];

                var result = SugarClient.get_entry_list(SessionID, "EmailAddresses", condition, "date_modified asc", 0, fieldsToRetrieve, entryList, 1, 0, false);


                for (int k = 0; k < fieldsToRetrieve.Length; k++)
                    table.Columns.Add(fieldsToRetrieve[k], typeof(string));

                for (int i = 0; i < result.result_count; i++)
                {
                    DataRow dtRow = table.NewRow();
                    for (int j = 0; j < fieldsToRetrieve.Length; j++)
                    {
                        dtRow[fieldsToRetrieve[j]] = result.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                    }
                    table.Rows.Add(dtRow);
                }

                if (table.Rows.Count > 0)
                {
                    return Convert.ToString(table.Rows[0]["id"]);
                }
                else
                {
                    return string.Empty;    
                }
            }
            catch (Exception ex)
            {
                throw ex;

            }
            return string.Empty;  

        }

        /// <summary>
        /// This Function  insert Follow up Emails data 
        /// </summary>
        /// <param name="lead_info"></param>
        public void set_web_email_replies_data(LeadInfo lead_info)
        {
            string RecordID = string.Empty;
            try
            {

                get_connection();

                if (SessionID != String.Empty)
                {
                    NameValueCollection fieldListCollection = new NameValueCollection();
                    //to update a record, you will nee to pass in a record id as commented below

                    fieldListCollection.Add("toemail", lead_info.toemail);
                    fieldListCollection.Add("fromemail", lead_info.fromemail);
                    fieldListCollection.Add("emailsubject", lead_info.emailsubject);
                    fieldListCollection.Add("emailbody", lead_info.emailbody);
                    fieldListCollection.Add("sendingdatetime", lead_info.sendingdatetime);
                    fieldListCollection.Add("name", lead_info.name);


                    //this is just a trick to avoid having to manually specify index values for name_value[]
                    name_value[] fieldList = new name_value[fieldListCollection.Count];

                    int count = 0;
                    foreach (string name in fieldListCollection)
                    {
                        foreach (string value in fieldListCollection.GetValues(name))
                        {
                            name_value field = new name_value();
                            field.name = name; field.value = value;
                            fieldList[count] = field;
                        }
                        count++;
                    }
                    new_set_entry_result result1 = SugarClient.set_entry(SessionID, "web_email_replies", fieldList);
                    RecordID = result1.id;
                    // RecordID = result1.ToString();
                    SugarClient.logout(SessionID);
                }
            }
            catch (Exception ex)
            {
                throw ex; //new Exception(RecordID.ToString());   

            }
        }

        //set optout to the user
        //public bool OptoutUser(int userid)
        //{
        //    bool isOptOut = false;
        //    UserInfo lead_info = UserController.GetUserById(0, userid);
        //    //check user existt in system
        //    if (lead_info != null)
        //    {


        //        UserMembership ums = new UserMembership();
        //        ums.Approved = false;
        //        ums.UpdatePassword = false;
        //        ums.IsOnLine = false;
        //        lead_info.Membership = ums;

        //        try
        //        {
        //            ProfileController.UpdateUserProfile(lead_info);
        //            UserController.UpdateUser(PortalSettings.PortalId, lead_info);
        //        }
        //        catch (Exception ex) { }

        //        //string qury = "update [UserPortals] set Authorised=0 where UserId=" + userid;
        //        //DotNetNuke.Data.DataProvider.Instance().ExecuteNonQuery("prc_property_search", qury);
        //        isOptOut = true;
        //    }
        //    return isOptOut;
        //}   

        private string GetIPAddress()
        {

            string sIPAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (sIPAddress == "")
            {
                sIPAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }
            return sIPAddress;
        }    

        private string AgentTemplate(string AgentPhoto, string AgentName, string AgentTitle, string AgentCellPhone, string AgentEmail, string AgentWebsite, string AgentWebsiteText, string SiteLogo, string TodayDate, string EmailText, string portalname)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + portalname + "/Email_Templates/AgentTemplate.html")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{AgentPhoto}", AgentPhoto);
            body = body.Replace("{AgentName}", AgentName);
            body = body.Replace("{AgentTitle}", AgentTitle);
            body = body.Replace("{AgentCellPhone}", AgentCellPhone);
            body = body.Replace("{AgentEmail}", AgentEmail);
            body = body.Replace("{AgentWebsite}", AgentWebsite);
            body = body.Replace("{AgentWebsiteText}", AgentWebsiteText);
            body = body.Replace("{SiteLogo}", SiteLogo);
            body = body.Replace("{TodayDate}", TodayDate);
            body = body.Replace("{EmailText}", EmailText);

            return body;
        }

        //public static void Main(string[] args)
        //{
        //    BA360_lib obj = new BA360_lib();
        //    LeadInfo lead_info = new LeadInfo();
        //    string result = "";
        //    try
        //    {
        //        lead_info.DnnLeadID = "15";
        //        lead_info.CompanyID = "842e39a5-3c9d-734b-af9b-537ee70083a3";

        //        result = obj.getleadinfo_new(lead_info);
        //        result = obj.set_OptOutInvalidEmail(11049, 0, "ExitRL", true, "");


        //        bool flag = true;

        //    }
        //    catch (Exception ex)
        //    {
        //        result += ex.Message.ToString() + ex.StackTrace.ToString();
        //    }
        //}


        public String getleadinfo_new(LeadInfo lead_info)
        {
            try
            {
                get_connection();

                var result = SugarClient.getLeadInfo(SessionID, Convert.ToInt32(lead_info.DnnLeadID), lead_info.CompanyID);

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return "0";
        }

        public String updateLeadLoginVisit(int frontendId, String companyId, Boolean flag)
        {
            var result = "0";
            try
            {
                get_connection();

                if (flag == true)
                    result = SugarClient.updateLeadLoginVisit(SessionID, frontendId, companyId, 1);
                else if (flag == false)
                    result = SugarClient.updateLeadLoginVisit(SessionID, frontendId, companyId, 0);

                if (result.ToString().Equals("Error"))
                {
                    return result;
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return result;
        }


        //getting list of agents from backend
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataTable GetTopLinks(cls_LinkGenerator cls_lg)
        {
            DataTable table;
            try
            {
                get_connection();
                //var fieldsToRetrieve = new[] { "id", "name", "date_entered", "date_modified", "modified_user_id", "created_by", "description", "deleted", 
                //    "assigned_user_id", "campaign_id_c", "company_id_c","generated_url_c", "page_title_c", "source_c", "tracker_url_c", "category_c" };

                var fieldsToRetrieve = new[] { "id", "name", "date_entered", "date_modified", "modified_user_id", "created_by", "description", "deleted", 
                    "assigned_user_id", "campaign_id_c", "company_id_c","generated_url_c", "page_title_c", "source_c",  "category_c","generatednew_url_c","trackernew_url_c" };

                link_name_to_fields_array[] entryList = new link_name_to_fields_array[0];   

                var result = SugarClient.getTopLinks(SessionID, cls_lg.CreatedBy, cls_lg.CompanyID, cls_lg.SourceList);
                table = new DataTable();

                for (int k = 0; k < fieldsToRetrieve.Length; k++)
                    table.Columns.Add(fieldsToRetrieve[k], typeof(string));

                for (int i = 0; i < result.topLinks.result_count; i++)
                {
                    DataRow dtRow = table.NewRow();
                    for (int j = 0; j < fieldsToRetrieve.Length; j++)
                    {
                        dtRow[fieldsToRetrieve[j]] = result.topLinks.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                    }
                    table.Rows.Add(dtRow);
                }
                SugarClient.logout(SessionID);
                return table;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;

        }

        public DataTable GetVendors(cls_LinkGenerator cls_lg)
        {
            DataTable table;
            try
            {
                get_connection();
                var fieldsToRetrieve = new[] { "id","salutation", "first_name", "last_name","name", "title", "phone_mobile", "phone_fax", "email1","email2", "description","assistant","assistant_phone",
                   "primary_address_street","primary_address_street_2","primary_address_street_3","primary_address_city","primary_address_state","primary_address_postalcode","primary_address_country",
                   "alt_address_street","alt_address_street_2","alt_address_street_3","alt_address_city","alt_address_state","alt_address_postalcode","alt_address_country",
                   "busssiness_lic_no_c", "company_logo_c", "company_url_c", "vendor_type_c","company_id_c","photo_c","phone_work" };

                link_name_to_fields_array[] entryList = new link_name_to_fields_array[0];

                var result = SugarClient.getVendors(SessionID, cls_lg.CreatedBy, cls_lg.CompanyID);
                table = new DataTable();

                for (int k = 0; k < fieldsToRetrieve.Length; k++)
                    table.Columns.Add(fieldsToRetrieve[k], typeof(string));

                for (int i = 0; i < result.vendor_list.result_count; i++)
                {
                    DataRow dtRow = table.NewRow();
                    for (int j = 0; j < fieldsToRetrieve.Length; j++)
                    {
                        dtRow[fieldsToRetrieve[j]] = result.vendor_list.entry_list[i].name_value_list.Where(nv => nv.name == fieldsToRetrieve[j]).ToArray()[0].value;
                    }
                    table.Rows.Add(dtRow);
                }
                SugarClient.logout(SessionID);
                return table;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;

        }

        public string create_Recruitment(RecruitmentInfo Recruitment_info)
        {
            try
            {
                get_connection();

                //create account -----------------------------------------------------------------------------------

                NameValueCollection fieldListCollection = new NameValueCollection();
                //to update a record, you will nee to pass in a record id as commented below

                fieldListCollection.Add("first_name", Recruitment_info.FirstName);
                fieldListCollection.Add("last_name", Recruitment_info.LastName);
                fieldListCollection.Add("phone_mobile", Recruitment_info.PhoneWork);
                fieldListCollection.Add("email1", Recruitment_info.EmailID);
                fieldListCollection.Add("company_id_c", Recruitment_info.CompanyID);
                fieldListCollection.Add("agent_referral_c", Recruitment_info.AgentReferral);
                //this is just a trick to avoid having to manually specify index values for name_value[]
                name_value[] fieldList = new name_value[fieldListCollection.Count];

                int count = 0;
                foreach (string name in fieldListCollection)
                {
                    foreach (string value in fieldListCollection.GetValues(name))
                    {
                        name_value field = new name_value();
                        field.name = name;
                        field.value = value;
                        fieldList[count] = field;
                    }
                    count++;
                }

                //creates new leads in backend
                new_set_entry_result result1 = SugarClient.set_entry(SessionID, "tbl_Recruitment", fieldList);
                SugarClient.logout(SessionID);
                return result1.id;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }
    }

}
