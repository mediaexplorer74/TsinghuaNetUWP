﻿#pragma checksum "C:\Users\media\source\repos\TsinghuaNet\TsinghuaNetUAP\TsinghuaNet\SignInDialog.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "BE11D1586067CB65689FAE5D98853AB0335DED21E8CAB90CCA65BF9FDAE002F8"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TsinghuaNet
{
    partial class SignInDialog : 
        global::Windows.UI.Xaml.Controls.ContentDialog, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.19041.685")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1: // SignInDialog.xaml line 1
                {
                    global::Windows.UI.Xaml.Controls.ContentDialog element1 = (global::Windows.UI.Xaml.Controls.ContentDialog)(target);
                    ((global::Windows.UI.Xaml.Controls.ContentDialog)element1).Loading += this.ContentDialog_Loading;
                    ((global::Windows.UI.Xaml.Controls.ContentDialog)element1).Closing += this.ContentDialog_Closing;
                }
                break;
            case 2: // SignInDialog.xaml line 15
                {
                    this.textBoxUserName = (global::Windows.UI.Xaml.Controls.TextBox)(target);
                    ((global::Windows.UI.Xaml.Controls.TextBox)this.textBoxUserName).TextChanged += this.textChanged;
                }
                break;
            case 3: // SignInDialog.xaml line 20
                {
                    this.passwordBoxPassword = (global::Windows.UI.Xaml.Controls.PasswordBox)(target);
                    ((global::Windows.UI.Xaml.Controls.PasswordBox)this.passwordBoxPassword).PasswordChanged += this.textChanged;
                }
                break;
            case 4: // SignInDialog.xaml line 26
                {
                    this.progressBar = (global::Windows.UI.Xaml.Controls.ProgressBar)(target);
                }
                break;
            case 5: // SignInDialog.xaml line 30
                {
                    this.textBlockHint = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.19041.685")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

