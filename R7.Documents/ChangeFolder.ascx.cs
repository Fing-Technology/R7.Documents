﻿//
// Copyright (c) 2014-2015 by Roman M. Yagodin <roman.yagodin@gmail.com>
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
using System.IO;
using System.Text;
using System.Collections;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.UI.UserControls;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using System.ComponentModel;
using System.Security.Cryptography;

namespace R7.Documents
{
    public enum SkippedDocumentsAction  { DoNothing, Unpublish, Delete, DeleteWithResources }

    public enum OldFilesAction { Keep, Delete }

	public partial class ChangeFolder : DocumentsPortalModuleBase
	{
		#region Fields
		
		#endregion
		
		#region Event Handlers

		protected override void OnInit (EventArgs e)
		{
			base.OnInit (e);

			// set folder to module's default folder
			if (DocumentsSettings.DefaultFolder != null)
				ddlFolder.SelectedFolder = FolderManager.Instance.GetFolder (DocumentsSettings.DefaultFolder.Value);

			cmdUpdate.Click += cmdUpdate_Click;
			linkCancel.NavigateUrl = Globals.NavigateURL ();
		}

		/*
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			try
			{
				if (!IsPostBack)
				{
				}
				else
				{
				}
			}
			catch (Exception ex)
			{
				// module failed to load
				Exceptions.ProcessModuleLoadException (this, ex);
			}
		}*/

		private void cmdUpdate_Click (object sender, EventArgs e)
		{
			try
			{
				var folder = ddlFolder.SelectedFolder;
				
				if (folder != null)
				{
					var documents = DocumentsController.GetDocuments (ModuleId, PortalId);
					var files = FolderManager.Instance.GetFiles (folder);

					foreach (var document in documents)
					{
						// only for files
						if (Globals.GetURLType (document.Url) == TabType.File)
						{
                            var docFileId = Utils.GetResourceId (document.Url);
							var docFile = FileManager.Instance.GetFile (docFileId);
							
							if (docFile != null)
							{
								var updated = false; 
								var oldDocument = document.Clone ();

								foreach (var file in files)
								{
									// case-insensitive comparison
									if (0 == string.Compare (file.FileName, docFile.FileName, StringComparison.InvariantCultureIgnoreCase))
									{
                                        document.Url = "FileID=" + file.FileId;
										document.CreatedDate = DateTime.Now;
										document.ModifiedDate = document.CreatedDate;
										document.CreatedByUserId = UserId;
										document.ModifiedByUserId = UserId;

										updated = true;
										break;
									}
								} // foreach 

                                if (updated)
                                {
                                    // publish updated documents
                                    document.IsPublished |= checkPublishUpdated.Checked;

                                    // safe remove old files, if needed.
                                    // need to do this before update!
                                    if (radioOldFilesAction.SelectedIndex == (int) OldFilesAction.Delete)
                                    {
                                        if (oldDocument.Url != document.Url)
                                        {
                                            DocumentsController.DeleteDocumentResource (oldDocument, PortalId);
                                        }
                                    }

                                    // update document & URL tracking data
                                    DocumentsController.Update (document);
                                    DocumentsController.UpdateDocumentUrl (document, oldDocument.Url, PortalId, ModuleId);
                                }
                                else
                                {
                                    switch ((SkippedDocumentsAction)radioSkippedAction.SelectedIndex) 
                                    {
                                        case SkippedDocumentsAction.Unpublish:
                                            // unpublish not updated documents & update them
                                            document.IsPublished = false;
                                            DocumentsController.Update (document);
                                            break;

                                        case SkippedDocumentsAction.Delete: 
                                            // delete not updated documents & URL tracking data
                                            DocumentsController.Delete (document);
                                            DocumentsController.DeleteDocumentUrl (oldDocument.Url, PortalId, ModuleId);
                                            break;

                                        case SkippedDocumentsAction.DeleteWithResources:
                                            // delete not updated documents, URL tracking data and resources
                                            DocumentsController.Delete (document);
                                            DocumentsController.DeleteDocumentUrl (oldDocument.Url, PortalId, ModuleId);
                                            DocumentsController.DeleteDocumentResource (document, PortalId);
                                            break;
                                    }
                                } // if (updated)
							}
						}
					} // foreach

					// update module's default folder setting
					if (checkUpdateDefaultFolder.Checked)
						DocumentsSettings.DefaultFolder = ddlFolder.SelectedFolder.FolderID;

                    Synchronize ();
				}

				// redirect back to the portal home page
				Response.Redirect (Globals.NavigateURL (), true);

			}
			catch (Exception ex)
			{
				// module failed to load
				Exceptions.ProcessModuleLoadException (this, ex);
			}
		}

		#endregion
	}
}
