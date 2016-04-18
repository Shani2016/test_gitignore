using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Web.Http;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security.Membership;
using System.Web.Security;
using System.Web;
using DotNetNuke.Security;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using DotNetNuke.Services.Authentication;
using System.Data;
using System.Collections;
using System.Xml.Linq;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Modules;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Mail;
using DotNetNuke.Services.Scheduling;  

namespace ba360lib
{
    class UpdateLeadBackendSchedular:SchedulerClient
    {
        public UpdateLeadBackendSchedular(ScheduleHistoryItem history)
            : base()
        {
            this.ScheduleHistoryItem = history;
        }
        public override void DoWork()
        {
            try
            {
                this.Progressing();

                //Your code goes here
                Add_LeadInfo_Backend alb = new Add_LeadInfo_Backend();
                alb.Check_Add_UnUpdated_Lead();

                //add To log notes 
                this.ScheduleHistoryItem.AddLogNote("Daily Report Suceess");

                //Show success
                this.ScheduleHistoryItem.Succeeded = true;


            }
            catch (Exception exc)
            {
                // on operation faild add exception in shedule History
                this.ScheduleHistoryItem.Succeeded = false;
                this.ScheduleHistoryItem.AddLogNote("EXCEPTION: " + exc.ToString());
                DotNetNuke.Services.Exceptions.Exceptions.LogException(exc);
            }
        }
    }

    class Add_LeadInfo_Backend
    {
       static string portalname = System.Web.Configuration.WebConfigurationManager.AppSettings["portalname"];
        string logfilepath = Path.Combine(HttpRuntime.AppDomainAppPath, "Portals/" + portalname + "/MLS_Medias/lead_activity_xml/Shedulers_log_Files/SendMailEx.txt");
        string companyid = System.Web.Configuration.WebConfigurationManager.AppSettings["companyID"];
        public void Check_Add_UnUpdated_Lead()
        {
            LeadInfo leadinfo = new LeadInfo();
            DataTable dtlead = new DataTable();
            BA360_lib balib = new BA360_lib();

            try
            {
                dtlead = balib.get_LeadList();


                if (dtlead.Rows.Count > 0)
                {
                    for (int i = 0; i < dtlead.Rows.Count; i++)
                    {
                        leadinfo.FirstName = Convert.ToString(dtlead.Rows[i]["FirstName"]);
                        leadinfo.LastName = Convert.ToString(dtlead.Rows[i]["LastName"]);
                        leadinfo.PhoneWork = Convert.ToString(dtlead.Rows[i]["PhoneWork"]);
                        leadinfo.EmailID = Convert.ToString(dtlead.Rows[i]["EmailID"]);
                        leadinfo.AgentID = Convert.ToString(dtlead.Rows[i]["AgentID"]);
                        leadinfo.LeadSource = Convert.ToString(dtlead.Rows[i]["LeadSource"]);
                        leadinfo.LenderID = Convert.ToString(dtlead.Rows[i]["LenderID"]);
                        leadinfo.CampaignID = Convert.ToString(dtlead.Rows[i]["CampaignID"]);
                        leadinfo.CampaignName = Convert.ToString(dtlead.Rows[i]["CampaignName"]);
                        leadinfo.Description = Convert.ToString(dtlead.Rows[i]["Description"]);
                        leadinfo.Status = Convert.ToString(dtlead.Rows[i]["Status"]);
                        leadinfo.AgentWebsite = Convert.ToString(dtlead.Rows[i]["AgentWebsite"]);
                        leadinfo.DnnLeadID = Convert.ToString(dtlead.Rows[i]["DnnLeadID"]);
                        leadinfo.CompanyID = Convert.ToString(dtlead.Rows[i]["CompanyID"]);
                        leadinfo.DnnPassword = Convert.ToString(dtlead.Rows[i]["DnnPassword"]);
                        leadinfo.OpportunityAmount = Convert.ToString(dtlead.Rows[i]["OpportunityAmount"]);
                        leadinfo.CreatedBY = Convert.ToString(dtlead.Rows[i]["CreatedBY"]);
                        leadinfo.Comment = Convert.ToString(dtlead.Rows[i]["Comment"]);
                        leadinfo.ActivityType = Convert.ToString(dtlead.Rows[i]["ActivityType"]);
                        leadinfo.Name = Convert.ToString(dtlead.Rows[i]["Name"]);
                        leadinfo.StatusForLender = Convert.ToString(dtlead.Rows[i]["StatusForLender"]);
                        leadinfo.form_views_c = Convert.ToString(dtlead.Rows[i]["form_views_c"]);
                        leadinfo.search_count_c = Convert.ToString(dtlead.Rows[i]["search_count_c"]);
                        leadinfo.toemail = Convert.ToString(dtlead.Rows[i]["toemail"]);
                        leadinfo.fromemail = Convert.ToString(dtlead.Rows[i]["fromemail"]);
                        leadinfo.emailsubject = Convert.ToString(dtlead.Rows[i]["emailsubject"]);
                        leadinfo.emailbody = Convert.ToString(dtlead.Rows[i]["emailbody"]);
                        leadinfo.name = Convert.ToString(dtlead.Rows[i]["name"]);
                        leadinfo.sendingdatetime = Convert.ToString(dtlead.Rows[i]["sendingdatetime"]);

                        balib.create_lead(leadinfo);
                        balib.updateLeadLoginVisit(Convert.ToInt32(leadinfo.DnnLeadID), companyid, true);
                        System.IO.File.AppendAllText(logfilepath, Environment.NewLine + leadinfo.EmailID.ToString() +" Updated to backend successfully");
                    }
                }

            }
            catch (Exception ex)
            {

            }

        }

     
    }
}
