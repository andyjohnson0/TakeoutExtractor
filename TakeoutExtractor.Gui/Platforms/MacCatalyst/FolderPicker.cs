using Foundation;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

// Based on code from MauiFolderPickerSample https://github.com/jfversluis/MauiFolderPickerSample
// and https://blog.verslu.is/maui/folder-picker-with-dotnet-maui/ by Gerald Versluis.
// Licenced Attribution-ShareAlike 4.0 International (CC BY-SA 4.0) https://creativecommons.org/licenses/by-sa/4.0/

namespace uk.andyjohnson.TakeoutExtractor.Gui.Platforms.MacCatalyst
{
    /// <summary>
    /// Mac-specific folder picker.
    /// </summary>
    public static class FolderPicker
    {
        /// <summary>
        /// Display a folder picker dialog.
        /// </summary>
        /// <returns>Selected directory, or null if no directory selected.</returns>
        public static async Task<DirectoryInfo?> PickFolderAsync()
        {
            var allowedTypes = new string[]
            {
                "public.folder"
            };

#pragma warning disable CA1416 // Validate platform compatibility
            // This call site is reachable on: 'MacCatalyst' 14.0 and later. 'UIDocumentPickerMode.Open' is unsupported on: 'maccatalyst' 14.0 and later.
            var picker = new UIDocumentPickerViewController(allowedTypes, UIDocumentPickerMode.Open);
#pragma warning restore CA1416 // Validate platform compatibility
            var tcs = new TaskCompletionSource<DirectoryInfo>();

            picker.Delegate = new PickerDelegate
            {
                PickHandler = urls => GetFileResults(urls!, tcs)
            };

            if (picker.PresentationController != null)
            {
                picker.PresentationController.Delegate =
                    new UIPresentationControllerDelegate(() => GetFileResults(null!, tcs));
            }

            var parentController = Platform.GetCurrentUIViewController();
            if (parentController != null)
                parentController.PresentViewController(picker, true, null);

            return await tcs.Task;
        }


        internal class PickerDelegate : UIDocumentPickerDelegate
        {
            public Action<NSUrl[]?>? PickHandler { get; set; }

            public override void WasCancelled(UIDocumentPickerViewController controller)
                => PickHandler?.Invoke(null);

            public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
                => PickHandler?.Invoke(urls);

            public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
                => PickHandler?.Invoke(new NSUrl[] { url });
        }


        static void GetFileResults(NSUrl[] urls, TaskCompletionSource<DirectoryInfo> tcs)
        {
            try
            {
                tcs.TrySetResult(new DirectoryInfo(urls?[0]?.ToString() ?? ""));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }



        internal class UIPresentationControllerDelegate : UIAdaptivePresentationControllerDelegate
        {
            Action? dismissHandler;

            internal UIPresentationControllerDelegate(Action dismissHandler)
                => this.dismissHandler = dismissHandler;

            public override void DidDismiss(UIPresentationController presentationController)
            {
                dismissHandler?.Invoke();
                dismissHandler = null;
            }

            protected override void Dispose(bool disposing)
            {
                dismissHandler?.Invoke();
                base.Dispose(disposing);
            }
        }
    }
}
