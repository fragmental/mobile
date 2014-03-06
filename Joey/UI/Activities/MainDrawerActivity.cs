using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Fragments;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true,
        Theme = "@style/Theme.Toggl.App")]
    public class MainDrawerActivity : BaseActivity
    {
        private static readonly string LogTag = "MainDrawerActivity";
        protected Logger log;
        private DrawerLayout DrawerLayout;
        protected ActionBarDrawerToggle DrawerToggle;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.MainDrawerActivity);

            var drawerList = FindViewById<ListView> (Resource.Id.DrawerListView);
            drawerList.Adapter = new DrawerListAdapter ();
            drawerList.ItemClick += OnDrawerItemClick;

            DrawerLayout = FindViewById<DrawerLayout> (Resource.Id.DrawerLayout);
            DrawerToggle = new ActionBarDrawerToggle (this, DrawerLayout, Resource.Drawable.IcDrawer, Resource.String.EntryName, Resource.String.EntryName);

            DrawerLayout.SetDrawerShadow (Resource.Drawable.drawershadow, (int)GravityFlags.Start);
            DrawerLayout.SetDrawerListener (DrawerToggle);

            ActionBar.SetDisplayHomeAsUpEnabled (true);
            ActionBar.SetHomeButtonEnabled (true);

            log = ServiceContainer.Resolve<Logger> ();
        }

        protected override void OnPostCreate (Bundle savedInstanceState)
        {
            base.OnPostCreate (savedInstanceState);
            DrawerToggle.SyncState ();
        }

        public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged (newConfig);
            DrawerToggle.OnConfigurationChanged (newConfig);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (DrawerToggle.OnOptionsItemSelected (item)) {
                return true;
            }

            return base.OnOptionsItemSelected (item);
        }

        protected override void OnResume ()
        {
            base.OnResume ();
            OpenTimeTracking ();
        }

        private void OpenTimeTracking ()
        {
            OpenFragment (new TimeTrackingFragment ());
        }

        private void OpenSettings ()
        {
            OpenFragment (new SettingsFragment ());
        }

        private void OpenFragment (Fragment fragment)
        {
            var fragmentTransaction = FragmentManager.BeginTransaction ();
            fragmentTransaction.Replace (Resource.Id.ContentFrameLayout, fragment).Commit ();
        }

        private void OnDrawerItemClick (object sender, ListView.ItemClickEventArgs e)
        {
            log.Debug (LogTag, "Drawer item clicked " + e.Id);

            if (e.Id == DrawerListAdapter.TimerPageId) {
                OpenTimeTracking ();

            } else if (e.Id == DrawerListAdapter.SettingsPageId) {
                OpenSettings ();

            }

            DrawerLayout.CloseDrawers ();
        }
    }
}

