using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Components;
using Toggl.Joey.UI.Fragments;
using ActionBar = Android.Support.V7.App.ActionBar;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentPagerAdapter = Android.Support.V4.App.FragmentPagerAdapter;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using ViewPager = Android.Support.V4.View.ViewPager;

namespace Toggl.Joey.UI.Fragments
{
    public class TimeTrackingFragment : Fragment
    {
        private static readonly int PagesCount = 3;
        private ViewPager viewPager;
        private TimerComponent timerSection = new TimerComponent ();

        public override void OnActivityCreated (Bundle savedInstanceState)
        {
            base.OnActivityCreated (savedInstanceState);

            var adapter = new MainPagerAdapter (Activity, ChildFragmentManager);
            viewPager = Activity.FindViewById<ViewPager> (Resource.Id.ViewPager);
            viewPager.Adapter = adapter;
            viewPager.CurrentItem = MainPagerAdapter.RecentPosition;
            viewPager.PageSelected += OnViewPagerPageSelected;

            timerSection.OnCreate (Activity);

            var lp = new ActionBar.LayoutParams (ActionBar.LayoutParams.WrapContent, ActionBar.LayoutParams.WrapContent);
            lp.Gravity = (int)(GravityFlags.Right | GravityFlags.CenterVertical);

            var actionBar = ((BaseActivity)Activity).ActionBar;
            actionBar.SetCustomView (timerSection.Root, lp);
            actionBar.SetDisplayShowCustomEnabled (true);

            // Make sure that the user will see newest data when they start the activity
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Full);
        }

        public override void OnDetach ()
        {
            base.OnDetach ();
        }

        public override void OnResume ()
        {
            base.OnResume ();
            viewPager.CurrentItem = MainPagerAdapter.RecentPosition;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate (Resource.Layout.TimeTrackingFragment, container, false);
        }

        private void OnViewPagerPageSelected (object sender, ViewPager.PageSelectedEventArgs e)
        {
            timerSection.HideDuration = e.Position == MainPagerAdapter.EditPosition;
        }

        public override void OnStart ()
        {
            base.OnStart ();
            timerSection.OnStart ();

            // Trigger a partial sync, if the sync from OnCreate is still running, it does nothing
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Auto);
        }

        public override void OnDestroy ()
        {
            base.OnStop ();
            timerSection.OnStop ();
        }

        private class MainPagerAdapter : FragmentPagerAdapter
        {
            public const int EditPosition = 0;
            public const int RecentPosition = 1;
            public const int LogPosition = 2;
            private Context ctx;
            private TimeEntryModel currentTimeEntry;
            #pragma warning disable 0414
            private readonly Subscription<ModelChangedMessage> subscriptionModelChanged;
            #pragma warning restore 0414
            private readonly EditCurrentTimeEntryFragment editFragment = new EditCurrentTimeEntryFragment ();
            private readonly RecentTimeEntriesListFragment recentFragment = new RecentTimeEntriesListFragment ();
            private readonly LogTimeEntriesListFragment logFragment = new LogTimeEntriesListFragment ();

            public MainPagerAdapter (Context ctx, FragmentManager fm) : base (fm)
            {
                this.ctx = ctx;

                currentTimeEntry = TimeEntryModel.FindRunning () ?? TimeEntryModel.GetDraft ();

                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                if (msg.Model == currentTimeEntry) {
                    // Listen for changes regarding current running entry
                    if (msg.PropertyName == TimeEntryModel.PropertyState
                        || msg.PropertyName == TimeEntryModel.PropertyStopTime
                        || msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                        if (currentTimeEntry.State == TimeEntryState.Finished || currentTimeEntry.DeletedAt.HasValue) {
                            currentTimeEntry = TimeEntryModel.GetDraft ();
                        }
                        NotifyDataSetChanged ();
                    }
                } else if (msg.Model is TimeEntryModel) {
                    // When some other time entry becomes IsRunning we need to switch over to that
                    if (msg.PropertyName == TimeEntryModel.PropertyState
                        || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                        var entry = (TimeEntryModel)msg.Model;
                        if (entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
                            currentTimeEntry = entry;
                            NotifyDataSetChanged ();
                        }
                    }
                }
            }

            public override int Count {
                get { return PagesCount; }
            }

            public override Java.Lang.ICharSequence GetPageTitleFormatted (int position)
            {
                var res = ctx.Resources;

                switch (position) {
                case EditPosition:
                    if (currentTimeEntry != null) {
                        if (currentTimeEntry.State == TimeEntryState.Running) {
                            return res.GetTextFormatted (Resource.String.TimeTrackingRunningTab);
                        } else if (currentTimeEntry.State == TimeEntryState.New && currentTimeEntry.StopTime.HasValue) {
                            return res.GetTextFormatted (Resource.String.TimeTrackingManualTab);
                        }
                    }
                    return res.GetTextFormatted (Resource.String.TimeTrackingNewTab);
                case RecentPosition:
                    // TODO: Determine if first run:
                    var firstRun = false;

                    if (firstRun) {
                        return res.GetTextFormatted (Resource.String.TimeTrackingWelcomeTab);
                    } else {
                        return res.GetTextFormatted (Resource.String.TimeTrackingRecentTab);
                    }
                case LogPosition:
                    return res.GetTextFormatted (Resource.String.TimeTrackingLogTab);
                default:
                    throw new InvalidOperationException ("Unknown tab position");
                }
            }

            public override Fragment GetItem (int position)
            {
                switch (position) {
                case EditPosition:
                    return editFragment;
                case RecentPosition:
                    return recentFragment;
                case LogPosition:
                    return logFragment;
                default:
                    throw new InvalidOperationException ("Unknown tab position");
                }
            }

            private static bool ForCurrentUser (TimeEntryModel model)
            {
                var authManager = ServiceContainer.Resolve<AuthManager> ();
                return model.UserId == authManager.UserId;
            }
        }
    }
}

