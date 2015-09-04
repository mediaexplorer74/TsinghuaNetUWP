﻿using Windows.ApplicationModel.Resources;

namespace TsinghuaNet
{
    public static class LocalizedStrings
    {
        private static readonly ResourceLoader loader = ResourceLoader.GetForViewIndependentUse();

        public static string AppVersionFormat
        {
            get;
        } = loader.GetString("AppVersionFormat"); 

        public static string Cancel
        {
            get;
        } = loader.GetString("Cancel"); 

        public static string EmptyPassword
        {
            get;
        } = loader.GetString("EmptyPassword"); 

        public static string EmptyUserName
        {
            get;
        } = loader.GetString("EmptyUserName"); 

        public static string Error
        {
            get;
        } = loader.GetString("Error"); 

        public static string Ok
        {
            get;
        } = loader.GetString("Ok"); 

        public static string PackageAuthor
        {
            get;
        } = loader.GetString("PackageAuthor"); 

        public static string PackageDescription
        {
            get;
        } = loader.GetString("PackageDescription"); 

        public static string PackageName
        {
            get;
        } = loader.GetString("PackageName"); 

        public static string ToastFailed
        {
            get;
        } = loader.GetString("ToastFailed"); 

        public static string ToastSuccess
        {
            get;
        } = loader.GetString("ToastSuccess"); 
    }
}