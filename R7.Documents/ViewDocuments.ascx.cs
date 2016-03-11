﻿//
// Copyright (c) 2002-2011 by DotNetNuke Corporation
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
using System.Linq;
using DotNetNuke;
using DotNetNuke.Common;
using DotNetNuke.Entities.Icons;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Actions;
using System.Collections;
using System.Collections.Generic;
using DotNetNuke.Services.Localization;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using System.Web.UI.WebControls;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Common.Utilities;
using DotNetNuke.R7;
using R7.Documents.Data;

namespace R7.Documents
{

	/// -----------------------------------------------------------------------------
	/// <summary>
	/// The Document Class provides the UI for displaying the Documents
	/// </summary>
	/// <remarks>
	/// </remarks>
	/// <history>
	/// 	[cnurse]	9/22/2004	Moved Documents to a separate Project
	/// </history>
	/// -----------------------------------------------------------------------------
	public partial class ViewDocuments : DocumentsPortalModuleBase, IActionable
	{
		private const int NOT_READ = -2;
		
		// private ArrayList mobjDocumentList;
		private List<DocumentInfo> mobjDocumentList;
		private int mintTitleColumnIndex = NOT_READ;
		private int mintDownloadLinkColumnIndex = NOT_READ;

		private bool mblnReadComplete = false;

		#region Event Handlers

        protected override void OnInit (EventArgs e)
        {
            base.OnInit (e);

            grdDocuments.AllowSorting = DocumentsSettings.AllowUserSort;

            // get grid style and apply to grid
            var style = GridStyle.Styles [DocumentsSettings.GridStyle];
            style.ApplyToGrid (grdDocuments);
        }

		/// <summary>
		/// OnLoad runs when the control is loaded
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			try
			{
				LoadData ();
			
				if (IsEditable && mobjDocumentList.Count == 0)
				{
                    this.Message ("NothingToDisplay.Text", MessageType.Info, true);
				}
				else if (!IsEditable && mobjDocumentList.Count (d => d.IsPublished) == 0)
				{
					ContainerControl.Visible = false;
				}
				else
				{
					LoadColumns ();
					grdDocuments.DataSource = mobjDocumentList;
					grdDocuments.DataBind ();
				}
			}
			catch (Exception exc)
			{
				// Module failed to load
				Exceptions.ProcessModuleLoadException (this, exc);
			}
		}

		/// <summary>
		/// Process user-initiated sort operation
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		/// <remarks></remarks>
		/// <history>
		/// 	[msellers]	5/17/2007	 Added
		/// </history>
        protected void grdDocuments_Sorting (object sender, GridViewSortEventArgs e)
		{
			ArrayList objCustomSortList = new ArrayList ();
			DocumentsSortColumnInfo objCustomSortColumn = new DocumentsSortColumnInfo ();
			DocumentsSortColumnInfo.SortDirection objCustomSortDirecton = DocumentsSortColumnInfo.SortDirection.Ascending;
			string strSortDirectionString = "ASC";

			// Set the sort column name
			objCustomSortColumn.ColumnName = e.SortExpression;

			// Determine if we need to reverse the sort.  This is needed if an existing sort on the same column existed that was desc
			if (ViewState ["CurrentSortOrder"] != null && ViewState ["CurrentSortOrder"].ToString () != string.Empty)
			{
				string existingSort = ViewState ["CurrentSortOrder"].ToString ();
				if (existingSort.StartsWith (e.SortExpression) && existingSort.EndsWith ("ASC"))
				{
					objCustomSortDirecton = DocumentsSortColumnInfo.SortDirection.Descending;
					strSortDirectionString = "DESC";
				}
			}

			// Set the sort
			objCustomSortColumn.Direction = objCustomSortDirecton;
			objCustomSortList.Add (objCustomSortColumn);

			var docComparer = new DocumentComparer (objCustomSortList);
			mobjDocumentList.Sort (docComparer.Compare);
			grdDocuments.DataSource = mobjDocumentList;
			grdDocuments.DataBind ();

			// Save the sort to viewstate
			ViewState ["CurrentSortOrder"] = e.SortExpression + " " + strSortDirectionString;

			// Mark as a user selected sort
			IsReadComplete = true;
		}

		/// <summary>
		/// If the datagrid was not sorted and bound via the "_Sort" method it will be bound at this time using
		/// default values
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPreRender (EventArgs e)
		{
			base.OnPreRender (e);
			
			// Only bind if not a user selected sort
			if (!IsReadComplete)
			{
				LoadData ();

				// Use DocumentComparer to do sort based on the default sort order (mobjSettings.SortOrder)
				var docComparer = new DocumentComparer (DocumentsSettings.GetSortColumnList (this.LocalResourceFile));
				mobjDocumentList.Sort (docComparer.Compare);

				//Bind the grid
				grdDocuments.DataSource = mobjDocumentList;
				grdDocuments.DataBind ();
			}

			// Localize the Data Grid
			// REVIEW: Original: Localization.LocalizeDataGrid(ref grdDocuments, this.LocalResourceFile);
            Localization.LocalizeGridView (ref grdDocuments, this.LocalResourceFile);
		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// grdDocuments_ItemCreated runs when an item in the grid is created
		/// </summary>
		/// <remarks>
		/// Set NavigateUrl for title, download links.  Also sets "scope" on 
		/// header rows so that text-to-speech readers can interpret the header row.
		/// </remarks>
		/// <history>
		/// 	[cnurse]	9/22/2004	Moved Documents to a separate Project
		/// </history>
		/// -----------------------------------------------------------------------------
		protected void grdDocuments_RowCreated (object sender, GridViewRowEventArgs e)
		{
			int intCount = 0;
			DocumentInfo objDocument = null;

			try
			{
				// hide edit column if not in edit mode
				if (!IsEditable)
					e.Row.Cells [0].Visible = false;  

				switch (e.Row.RowType)
				{
                    case DataControlRowType.Header:
						// set CSS class for edit column header
						e.Row.Cells [0].CssClass = "EditHeader";

						// Setting "scope" to "col" indicates to for text-to-speech
						// or braille readers that this row containes headings
						for (intCount = 1; intCount <= e.Row.Cells.Count - 1; intCount++)
						{
							e.Row.Cells [intCount].Attributes.Add ("scope", "col");
						}
						break;

                    case DataControlRowType.DataRow:
                    	// If ShowTitleLink is true, the title column is generated dynamically
						// as a template, which we can't data-bind, so we need to set the text
						// value here
                        objDocument = (DocumentInfo)mobjDocumentList [e.Row.RowIndex];

						// set CSS class for edit column cells
						e.Row.Cells [0].CssClass = "EditCell";

						// decorate unpublished items
						if (!objDocument.IsPublished)
						{
                            e.Row.CssClass = ((e.Row.RowIndex % 2 == 0)? grdDocuments.RowStyle.CssClass
                                : grdDocuments.AlternatingRowStyle.CssClass) + " _nonpublished";
						}

						if (DocumentsSettings.ShowTitleLink)
						{
							if (mintTitleColumnIndex == NOT_READ)
							{
								mintTitleColumnIndex = DocumentsSettings.FindGridColumn (DocumentsDisplayColumnInfo.COLUMN_TITLE, DocumentsSettings.DisplayColumnList, true);
							}

							if (mintTitleColumnIndex >= 0)
							{
								// Dynamically set the title link URL
								var _with1 = (HyperLink)e.Row.Controls [mintTitleColumnIndex + 1].FindControl ("ctlTitle");
								_with1.Text = objDocument.Title;
								
								// set link title to display document description
								_with1.ToolTip = objDocument.Description;

								// Note: The title link should display inline if possible, so set
								// ForceDownload=False
								_with1.NavigateUrl = Globals.LinkClick (objDocument.Url, TabId, ModuleId, objDocument.TrackClicks, objDocument.ForceDownload);
								if (objDocument.NewWindow)
								{
									_with1.Target = "_blank";
								}

                                // set HTML attributes for the link
                                var docFormatter = new DocumentInfoFormatter (objDocument);
                                foreach (var htmlAttr in docFormatter.LinkAttributesCollection)
                                    _with1.Attributes.Add (htmlAttr.Item1, htmlAttr.Item2);
							}
						}

						// If there's a "download" link, set the NavigateUrl 
						if (mintDownloadLinkColumnIndex == NOT_READ)
						{
							mintDownloadLinkColumnIndex = DocumentsSettings.FindGridColumn (DocumentsDisplayColumnInfo.COLUMN_DOWNLOADLINK, DocumentsSettings.DisplayColumnList, true);
						}
						if (mintDownloadLinkColumnIndex >= 0)
						{
							var _with2 = (HyperLink)e.Row.Controls [mintDownloadLinkColumnIndex].FindControl ("ctlDownloadLink");
							// Note: The title link should display open/save dialog if possible, 
							// so set ForceDownload=True
							_with2.NavigateUrl = Globals.LinkClick (objDocument.Url, TabId, ModuleId, objDocument.TrackClicks, objDocument.ForceDownload);
							if (objDocument.NewWindow)
							{
								_with2.Target = "_blank";
							}

                            // display clicks in the tooltip
                            if (objDocument.Clicks >= 0)
                                _with2.ToolTip = string.Format (LocalizeString ("Clicks.Format"), objDocument.Clicks);
						}
						break;
				}

				//Module failed to load
			}
			catch (Exception exc)
			{
				Exceptions.ProcessModuleLoadException (this, exc);
			}
		}

		#endregion

		#region IActionable implementation

		public ModuleActionCollection ModuleActions
		{
			get
			{
				ModuleActionCollection Actions = new ModuleActionCollection ();
				
				Actions.Add (GetNextActionID (), 
					Localization.GetString (ModuleActionType.AddContent, LocalResourceFile), 
					ModuleActionType.AddContent, "", "", EditUrl (), false, SecurityAccessLevel.Edit, true, false);

				Actions.Add (GetNextActionID (), 
					Localization.GetString ("ChangeFolder.Action", LocalResourceFile),
					"ChangeFolder.Action", "", "", EditUrl ("ChangeFolder"), false, SecurityAccessLevel.Edit, true, false);

				Actions.Add (GetNextActionID (), 
					Localization.GetString ("ImportDocuments.Action", LocalResourceFile),
					"ImportDocuments.Action", "", "", EditUrl ("ImportDocuments"), false, SecurityAccessLevel.Edit, true, false);
			
				return Actions;
			}
		}

		#endregion

		#region Private Methods

		private void LoadColumns ()
		{
			DocumentsDisplayColumnInfo objDisplayColumn = null;

			// Add columns dynamically
			foreach (DocumentsDisplayColumnInfo objDisplayColumn_loopVariable in DocumentsSettings.DisplayColumnList)
			{
				objDisplayColumn = objDisplayColumn_loopVariable;

				if (objDisplayColumn.Visible)
				{
					switch (objDisplayColumn.ColumnName)
					{
						case DocumentsDisplayColumnInfo.COLUMN_CATEGORY:
							AddDocumentColumn (Localization.GetString ("Category", LocalResourceFile), "Category", "Category");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_CREATEDBY:
							AddDocumentColumn (Localization.GetString ("CreatedBy", LocalResourceFile), "CreatedBy", "CreatedByUser", "");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_CREATEDDATE:
							AddDocumentColumn (Localization.GetString ("CreatedDate", LocalResourceFile), "CreatedDate", "CreatedDate", "{0:d}");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_DESCRIPTION:
							AddDocumentColumn (Localization.GetString ("Description", LocalResourceFile), "Description", "Description");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_DOWNLOADLINK:
							AddDownloadLink ("DownloadLink", "DownloadLink", "DownloadLink", "ctlDownloadLink");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_MODIFIEDBY:
							AddDocumentColumn (Localization.GetString ("ModifiedBy", LocalResourceFile), "ModifiedBy", "ModifiedByUser");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_MODIFIEDDATE:
							AddDocumentColumn (Localization.GetString ("ModifiedDate", LocalResourceFile), "ModifiedDate", "ModifiedDate", "{0:d}");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_OWNEDBY:
							AddDocumentColumn (Localization.GetString ("Owner", LocalResourceFile), "Owner", "OwnedByUser");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_SIZE:
							AddDocumentColumn (Localization.GetString ("Size", LocalResourceFile), "Size", "FormatSize");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_CLICKS:
							AddDocumentColumn (Localization.GetString ("Clicks", LocalResourceFile), "Clicks", "Clicks");

							break;
						case DocumentsDisplayColumnInfo.COLUMN_ICON:
							AddDocumentColumn (Localization.GetString ("Icon", LocalResourceFile), "Icon", "FormatIcon");

							break;
					// case DocumentsDisplayColumnInfo.COLUMN_URL:
					// AddDocumentColumn (Localization.GetString ("Url", LocalResourceFile), "Url", "FormatUrl");
					// break;

						case DocumentsDisplayColumnInfo.COLUMN_TITLE:
							if (DocumentsSettings.ShowTitleLink)
							{
								AddDownloadLink (Localization.GetString ("Title", LocalResourceFile), "Title", "Title", "ctlTitle");
							}
							else
							{
								AddDocumentColumn (Localization.GetString ("Title", LocalResourceFile), "Title", "Title");
							}
							break;
					}
				}
			}
		}

		private void LoadData ()
		{
			string strCacheKey = null;
			
			if (IsReadComplete)
				return;

			// Only read from the cache if the users is not logged in
			strCacheKey = this.DataCacheKey + ";anon-doclist";
			if (!Request.IsAuthenticated)
			{
				mobjDocumentList = (List<DocumentInfo>)DataCache.GetCache (strCacheKey);
			}

			if (mobjDocumentList == null)
			{
				//	mobjDocumentList = (ArrayList) DocumentsController.GetObjects<DocumentInfo>(ModuleId); // PortalId!!!

				mobjDocumentList = DocumentsDataProvider.Instance.GetDocuments (ModuleId, PortalId).ToList ();

				// Check security on files
				DocumentInfo objDocument = null;

                for (var intCount = mobjDocumentList.Count - 1; intCount >= 0; intCount--)
				{
					objDocument = mobjDocumentList [intCount];
                    if (objDocument.Url.IndexOf ("fileid=", StringComparison.InvariantCultureIgnoreCase) >= 0)
					{
						// document is a file, check security
						var objFile = FileManager.Instance.GetFile (int.Parse (objDocument.Url.Split ('=') [1]));
					
						//if ((objFile != null) && !PortalSecurity.IsInRoles(FileSystemUtils.GetRoles(objFile.Folder, PortalSettings.PortalId, "READ"))) {
						if (objFile != null)
						{
							var folder = FolderManager.Instance.GetFolder (objFile.FolderId);
                            if (folder != null && !FolderPermissionController.CanViewFolder ((FolderInfo)folder))
							{
								// remove document from the list
                                mobjDocumentList.Remove (objDocument);
								continue;
							}
						}
					}
					
					// remove unpublished documents from the list
					if (!objDocument.IsPublished && !IsEditable)
					{
						mobjDocumentList.Remove (objDocument);
						continue;
					}

					objDocument.OnLocalize += new LocalizeHandler (OnLocalize);
				}

				// Only write to the cache if the user is not logged in
				if (!Request.IsAuthenticated)
				{
					DataCache.SetCache (strCacheKey, mobjDocumentList, new TimeSpan (0, 5, 0));
				}
			}

			//Sort documents
			var docComparer = new DocumentComparer (DocumentsSettings.GetSortColumnList (this.LocalResourceFile));
			mobjDocumentList.Sort (docComparer.Compare);

			IsReadComplete = true;
		}

		private string OnLocalize (string text)
		{
			return Localization.GetString (text, this.LocalResourceFile);
		}

		private bool IsReadComplete
		{
			get { return mblnReadComplete; }
			set { mblnReadComplete = value; }
		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// Dynamically adds a column to the datagrid
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="Title">The name of the property to read data from</param>
		/// <param name="DataField">The name of the property to read data from</param>
		/// -----------------------------------------------------------------------------
		private void AddDocumentColumn (string Title, string CssClass, string DataField)
		{
			AddDocumentColumn (Title, CssClass, DataField, "");
		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// Dynamically adds a column to the datagrid
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="Title">The name of the property to read data from</param>
		/// <param name="DataField">The name of the property to read data from</param>
		/// <param name="Format">Format string for value</param>
		/// -----------------------------------------------------------------------------
		private void AddDocumentColumn (string Title, string CssClass, string DataField, string Format)
		{
            var objBoundColumn = new BoundField ();

            // don't HTML encode icons markup
            if (DataField == "FormatIcon")
                objBoundColumn.HtmlEncode = false;

			objBoundColumn.DataField = DataField;
			objBoundColumn.DataFormatString = Format;
			objBoundColumn.HeaderText = Title;
			//Added 5/17/2007
			//By Mitchel Sellers
			if (DocumentsSettings.AllowUserSort)
			{
				objBoundColumn.SortExpression = DataField;
			}

			objBoundColumn.HeaderStyle.CssClass = CssClass + "Header";
			//"NormalBold"
			objBoundColumn.ItemStyle.CssClass = CssClass + "Cell";
			//"Normal"

            this.grdDocuments.Columns.Add (objBoundColumn);

		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// Dynamically adds a DownloadColumnTemplate column to the datagrid.  Used to
		/// add the download link and title (if "title as link" is set) columns.
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="Title">The name of the property to read data from</param>
		/// <param name="Name">The name of the property to read data from</param>
		/// -----------------------------------------------------------------------------
		private void AddDownloadLink (string Title, string CssClass, string DataField, string Name)
		{
			var objTemplateColumn = new TemplateField ();
			objTemplateColumn.ItemTemplate = new DownloadColumnTemplate (Name, Localization.GetString ("DownloadLink.Text", LocalResourceFile), ListItemType.Item);
			
			if (Name == "ctlDownloadLink")
				objTemplateColumn.HeaderText = "";
			else
				objTemplateColumn.HeaderText = Title;
			
			objTemplateColumn.HeaderStyle.CssClass = CssClass + "Header";
			//"NormalBold"
			objTemplateColumn.ItemStyle.CssClass = CssClass + "Cell";
			//"Normal"

			//Added 5/17/2007
			//By Mitchel Sellers
			// Add the sort expression, however ensure that it is NOT added for download
			if (DocumentsSettings.AllowUserSort && !Name.Equals ("ctlDownloadLink"))
			{
				objTemplateColumn.SortExpression = DataField;
			}
			this.grdDocuments.Columns.Add (objTemplateColumn);
		}

		protected string EditImageUrl
		{
			get { return IconController.IconURL ("Edit"); } 
		}

		/*
		/// -----------------------------------------------------------------------------
		/// <summary>
		/// Load module settings from the database.
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// -----------------------------------------------------------------------------
		private DocumentsSettingsInfo LoadSettings()
		{
			DocumentsSettingsInfo objDocumentsSettings = null;
			// Load module instance settings
			var _with3 = new DocumentsController();
			objDocumentsSettings = _with3.GetDocumentsSettings(ModuleId);

			// first time around, no existing documents settings will exist
			if (objDocumentsSettings == null) {
				objDocumentsSettings = new DocumentsSettingsInfo();
			}

			return objDocumentsSettings;
		}
*/
		#endregion
	}
}
