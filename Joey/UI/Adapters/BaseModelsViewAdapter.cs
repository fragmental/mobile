﻿using System;
using System.ComponentModel;
using System.Linq;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Joey.UI.Adapters
{
    public abstract class BaseModelsViewAdapter<T> : BaseAdapter
        where T : Model, new()
    {
        private static readonly int LoadMoreOffset = 3;
        protected static readonly int ViewTypeLoaderPlaceholder = 0;
        protected static readonly int ViewTypeContent = 1;
        private IModelsView<T> modelsView;

        public BaseModelsViewAdapter (IModelsView<T> modelsView)
        {
            this.modelsView = modelsView;
            modelsView.PropertyChanged += OnModelsViewPropertyChanged;
        }

        private void OnModelsViewPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            // Need to access the Handle property, else mono optimises/loses the context and we get a weird
            // low-level exception about "'jobject' must not be IntPtr.Zero".
            if (Handle == IntPtr.Zero)
                return;
            if (e.PropertyName == ModelsView<T>.PropertyHasMore
                || e.PropertyName == ModelsView<T>.PropertyModels
                || e.PropertyName == ModelsView<T>.PropertyIsLoading) {
                NotifyDataSetChanged ();
            }
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return null;
        }

        public virtual T GetModel (int position)
        {
            if (modelsView.IsLoading && position == modelsView.Count)
                return null;
            return modelsView.Models.ElementAt (position);
        }

        public override long GetItemId (int position)
        {
            return position;
        }

        public override bool HasStableIds {
            get { return false; }
        }

        public override int GetItemViewType (int position)
        {
            if (position == modelsView.Count && modelsView.IsLoading)
                return ViewTypeLoaderPlaceholder;

            return ViewTypeContent;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            if (position + LoadMoreOffset > Count && modelsView.HasMore && !modelsView.IsLoading) {
                modelsView.LoadMore ();
            }

            var viewType = GetItemViewType (position);
            if (viewType == ViewTypeLoaderPlaceholder) {
                return GetLoadIndicatorView (position, convertView, parent);
            } else {
                return GetModelView (position, convertView, parent);
            }
        }

        protected virtual View GetLoadIndicatorView (int position, View convertView, ViewGroup parent)
        {
            if (convertView != null)
                return convertView;
            // TODO: Implement default loading indicator view
            var view = LayoutInflater.FromContext (parent.Context).Inflate (
                           Resource.Layout.TimeEntryListLoadingItem, parent, false);

            ImageView spinningImage = view.FindViewById<ImageView> (Resource.Id.LoadingImageView);
            Animation spinningImageAnimation = AnimationUtils.LoadAnimation (parent.Context, Resource.Animation.SpinningAnimation);
            spinningImage.StartAnimation (spinningImageAnimation);

            return view;
        }

        protected abstract View GetModelView (int position, View convertView, ViewGroup parent);

        public override int ViewTypeCount {
            get { return 2; }
        }

        public override int Count {
            get {
                if (modelsView.IsLoading)
                    return (int)modelsView.Count + 1;
                return (int)modelsView.Count;
            }
        }

        protected IModelsView<T> ModelsView {
            get { return modelsView; }
        }
    }
}
