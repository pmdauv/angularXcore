﻿namespace Angular2Core.Models
{
    public class Application
    {
        public int ApplicationId { get; set; }
        public string DisplayName { get; set; }
        public string RedirectUri { get; set; }
        public string LogoutRedirectUri { get; set; }
        public string Secret { get; set; }
    }
}