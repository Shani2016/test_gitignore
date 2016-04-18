using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ba360lib
{
    public class MeetingInfo
    {
        public string Name { get; set; }
        public string AssignAgentID { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string DurationHours { get; set; }
        public string DurationMinutes { get; set; }
        public string DateStart { get; set; }
        public string DateEnd { get; set; }
        public string ParentType { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string LeadID { get; set; }
        public string ReminderTime { get; set; }
        public string EmailReminderTime { get; set; }
        public string CompanyID { get; set; }
    }
}
