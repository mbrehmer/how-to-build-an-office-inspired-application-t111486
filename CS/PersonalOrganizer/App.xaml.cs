﻿using DevExpress.Xpf.Core;
using PersonalOrganizer.Model;
using System.Data.Entity;
using System.Windows;

namespace PersonalOrganizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ApplicationThemeHelper.ApplicationThemeName = Theme.Office2013.Name;
            Database.SetInitializer(new ContactContextInitializer());
            base.OnStartup(e);
        }
    }
}