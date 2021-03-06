﻿//
// DocumentViewModel.cs
//
// Author:
//       Roman M. Yagodin <roman.yagodin@gmail.com>
//
// Copyright (c) 2017 Roman M. Yagodin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Icons;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Localization;
using R7.Dnn.Extensions.Models;
using R7.Dnn.Extensions.Utilities;
using R7.Dnn.Extensions.ViewModels;
using R7.Documents.Helpers;
using R7.Documents.Models;

namespace R7.Documents.ViewModels
{
    public class DocumentViewModel : IDocumentViewModel
    {
        protected IDocument Model;

        protected ViewModelContext<DocumentsSettings> Context;

        public DocumentViewModel (IDocument document, ViewModelContext<DocumentsSettings> context)
        {
            Model = document;
            Context = context;
        }

        #region IDocumentViewModel implementation

        public string Category => Model.Category;

        public int Clicks => Model.Clicks;

        public int CreatedByUserId => Model.CreatedByUserId;

        public DateTime CreatedDate => Model.CreatedDate;

        public string Description => Model.Description;

        public DateTime? EndDate => Model.EndDate;

        public bool ForceDownload => Model.ForceDownload;

        public int ItemId => Model.ItemId;

        public string LinkAttributes => Model.LinkAttributes;

        public int ModifiedByUserId => Model.ModifiedByUserId;

        public DateTime ModifiedDate => Model.ModifiedDate;

        public int ModuleId => Model.ModuleId;

        public bool NewWindow => Model.NewWindow;

        public int OwnedByUserId => Model.OwnedByUserId;

        public int Size => Model.Size;

        public int SortOrderIndex => Model.SortOrderIndex;

        public DateTime? StartDate => Model.StartDate;

        public string Title => Model.Title;

        public bool TrackClicks => Model.TrackClicks;

        public string Url => Model.Url;

        public DateTime PublishedOnDate {
            get {
                return ModelHelper.PublishedOnDate (StartDate, CreatedDate);
            }
        }

        string _createdByUser;
        public string CreatedByUser => _createdByUser ?? (_createdByUser = UserHelper.GetUserDisplayName (Context.Module.PortalId, Model.CreatedByUserId));

        string _modifiedByUser;
        public string ModifiedByUser => _modifiedByUser ?? (_modifiedByUser = UserHelper.GetUserDisplayName (Context.Module.PortalId, Model.ModifiedByUserId));

        string _ownedByUser;
        public string OwnedByUser => _ownedByUser ?? (_ownedByUser = UserHelper.GetUserDisplayName (Context.Module.PortalId, Model.OwnedByUserId));

        #endregion

        string _formatSize;
        public string FormatSize {
            get {
                if (_formatSize == null) {
                    try {
                        if (Size > 0) {
                            if (Size > (1024 * 1024)) {
                                _formatSize = string.Format ("{0:#,##0.00} {1}", Size / 1024f / 1024f, Localization.GetString ("Megabytes.Text", Context.LocalResourceFile));
                            } else {
                                _formatSize = string.Format ("{0:#,##0.00} {1}", Size / 1024f, Localization.GetString ("Kilobytes.Text", Context.LocalResourceFile));
                            }
                        }
                    } catch (Exception ex) {
                        Exceptions.LogException (ex);
                    }
                    if (_formatSize == null) {
                        _formatSize = Localization.GetString ("Unknown.Text", Context.LocalResourceFile);
                    }
                }
                return _formatSize;
            }
        }

        string _formatIcon;
        public string FormatIcon {
            get {
                if (_formatIcon == null) {
                    _formatIcon = "";

                    switch (Globals.GetURLType (Url)) {
                    case TabType.File:

                        var fileId = TypeUtils.ParseToNullable<int> (Url.ToLowerInvariant ().Substring ("fileid=".Length));
                        if (fileId != null) {
                            var fileInfo = FileManager.Instance.GetFile (fileId.Value);
                            if (fileInfo != null && !string.IsNullOrWhiteSpace (fileInfo.Extension)) {
                                // optimistic way 
                                _formatIcon = string.Format (
                                    "<img src=\"{0}\" alt=\"{1}\" title=\"{1}\" />",
                                    IconController.IconURL ("Ext" + fileInfo.Extension),
                                    fileInfo.Extension.ToUpperInvariant ());
                            }
                        }
                        break;

                    case TabType.Tab:
                        _formatIcon = string.Format ("<img src=\"{0}\" alt=\"{1}\" title=\"{1}\" />",
                                                     IconController.IconURL ("Tabs", "16X16"), Localization.GetString ("Page.Text", Context.LocalResourceFile));
                        break;

                    default:
                        _formatIcon = string.Format ("<img src=\"{0}\" alt=\"{1}\" title=\"{1}\" />",
                                                     IconController.IconURL ("Link", "16X16", "Gray"), "URL");
                        break;
                    }
                }

                return _formatIcon;
            }
        }

        string _toolTip;
        public string ToolTip {
            get {
                if (_toolTip == null) {
                    _toolTip = "";

                    switch (Globals.GetURLType (Url)) {
                        case TabType.File:
                            var fileId = TypeUtils.ParseToNullable<int> (Url.ToLowerInvariant ().Substring ("fileid=".Length));
                            if (fileId != null) {
                                var fileInfo = FileManager.Instance.GetFile (fileId.Value);
                                if (fileInfo != null) {
                                    _toolTip = fileInfo.RelativePath;
                                }
                            }
                            break;

                        case TabType.Tab:
                            var tabId = TypeUtils.ParseToNullable<int> (Url);
                            if (tabId != null) {
                                var tabInfo = TabController.Instance.GetTab (tabId.Value, Null.NullInteger);
                                if (tabInfo != null) {
                                    // TODO: Show LocalizedTabName instead?
                                    _toolTip = tabInfo.TabName;
                                }
                            }
                            break;

                        default:
                            _toolTip = Url;
                            break;
                    }
                }

                return _toolTip;
            }
        }
    }
}
