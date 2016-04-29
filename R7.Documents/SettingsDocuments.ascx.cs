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
using System.Collections;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using DotNetNuke.Entities.Icons;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Common.Lists;
using DotNetNuke.Common.Utilities;
using R7.DotNetNuke.Extensions.Modules;
using R7.DotNetNuke.Extensions.ControlExtensions;

namespace R7.Documents
{
	/// -----------------------------------------------------------------------------
	/// <summary>
	/// The EditDocs Class provides the UI for manaing the Documents
	/// </summary>
	/// <remarks>
	/// </remarks>
	/// <history>
	/// 	[cnurse]	9/22/2004	Moved Documents to a separate Project
	/// </history>
	/// -----------------------------------------------------------------------------
    public partial class SettingsDocuments : ModuleSettingsBase<DocumentsSettings>
	{
		private const string VIEWSTATE_SORTCOLUMNSETTINGS = "SortColumnSettings";

		private const string VIEWSTATE_DISPLAYCOLUMNSETTINGS = "DisplayColumnSettings";

		#region Event Handlers

		protected override void OnInit (EventArgs e)
		{
			base.OnInit (e);

			// fill sort order direction combobox
			comboSortOrderDirection.AddItem (LocalizeString ("SortOrderAscending.Text"), "ASC");
			comboSortOrderDirection.AddItem (LocalizeString ("SortOrderDescending.Text"), "DESC");

            // bind grid styles
            comboGridStyle.DataSource = GridStyle.Styles.Values;
            comboGridStyle.DataBind ();
		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// LoadSettings loads the settings from the Databas and displays them
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <history>
		/// </history>
		/// -----------------------------------------------------------------------------
		public override void LoadSettings ()
		{
			DocumentsDisplayColumnInfo objColumnInfo = null;
			
			try
			{
				if (!IsPostBack)
				{
					LoadLists ();

					chkShowTitleLink.Checked = Settings.ShowTitleLink;
					chkUseCategoriesList.Checked = Settings.UseCategoriesList;
					chkAllowUserSort.Checked = Settings.AllowUserSort;
                    comboGridStyle.SelectByValue (Settings.GridStyle);

					try
					{
						if (Settings.DefaultFolder != null)
						{
							folderDefaultFolder.SelectedFolder = 
								FolderManager.Instance.GetFolder (Settings.DefaultFolder.Value);
						}
					}
					catch
					{
						// suppress exception.  Can be caused if the selected folder has been deleted
					}

					try
					{
						cboCategoriesList.SelectedValue = Settings.CategoriesListName;
					}
					catch
					{
						// suppress exception.  Can be caused if the selected list has been deleted
					}

					// read "saved" column sort orders in first
					var objColumnSettings = Settings.DisplayColumnList;

					foreach (DocumentsDisplayColumnInfo objColumnInfo_loopVariable in objColumnSettings)
					{
						objColumnInfo = objColumnInfo_loopVariable;
						// Set localized column names
						objColumnInfo.LocalizedColumnName = Localization.GetString (objColumnInfo.ColumnName + ".Header", base.LocalResourceFile);
					}
					
					// Add any missing columns to the end
					foreach (string strColumnName_loopVariable in DocumentsDisplayColumnInfo.AvailableDisplayColumns)
					{
						var strColumnName = strColumnName_loopVariable;
						if (DocumentsSettings.FindColumn (strColumnName, objColumnSettings, false) < 0)
						{
							objColumnInfo = new DocumentsDisplayColumnInfo ();
							objColumnInfo.ColumnName = strColumnName;
							objColumnInfo.LocalizedColumnName = Localization.GetString (objColumnInfo.ColumnName + ".Header", base.LocalResourceFile);
							objColumnInfo.DisplayOrder = objColumnSettings.Count + 1;
							objColumnInfo.Visible = false;
							
							objColumnSettings.Add (objColumnInfo);
						}
					}

					// Sort by DisplayOrder
					BindColumnSettings (objColumnSettings);

					// Load sort columns 
					string strSortColumn = null;
					foreach (string strSortColumn_loopVariable in DocumentsDisplayColumnInfo.AvailableSortColumns)
					{
						strSortColumn = strSortColumn_loopVariable;
						comboSortFields.AddItem (LocalizeString (strSortColumn + ".Header"), strSortColumn);
					}

					BindSortSettings (Settings.GetSortColumnList (this.LocalResourceFile));

                    // load grid style
                    comboGridStyle.SelectByValue (Settings.GridStyle);
				}
				//Module failed to load
			}
			catch (Exception exc)
			{
				Exceptions.ProcessModuleLoadException (this, exc);
			}
		}

		public void LoadLists ()
		{
            var _with2 = new ListController ();
			foreach (ListInfo objList in _with2.GetListInfoCollection())
			{
				if (!objList.SystemList)
				{
					// for some reason, the "DataType" is not marked as a system list, but we want to exclude that one too
					if (objList.DisplayName != "DataType")
					{
						cboCategoriesList.Items.Add (new ListItem (objList.DisplayName, objList.DisplayName));
					}
				}
			}

			if (cboCategoriesList.Items.Count == 0)
			{
				lstNoListsAvailable.Text = Localization.GetString ("msgNoListsAvailable.Text", base.LocalResourceFile);
				lstNoListsAvailable.Visible = true;
			}
		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// UpdateSettings saves the modified settings to the Database
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <history>
		/// </history>
		/// -----------------------------------------------------------------------------
		public override void UpdateSettings ()
		{
			try
			{
				if (Page.IsValid)
				{
					FillSettings ();

                    ModuleSynchronizer.Synchronize (ModuleId, TabModuleId);
				}
			}
			catch (Exception exc)
			{
                // module failed to load
				Exceptions.ProcessModuleLoadException (this, exc);
			}
		}

		public string GetLocalizedText (string Key)
		{
			return Localization.GetString (Key, base.LocalResourceFile);
		}

		protected void grdSortColumns_ItemCreated (object sender, System.Web.UI.WebControls.DataGridItemEventArgs e)
		{
			switch (e.Item.ItemType)
			{
				case System.Web.UI.WebControls.ListItemType.AlternatingItem:
				case System.Web.UI.WebControls.ListItemType.Item:
				case System.Web.UI.WebControls.ListItemType.SelectedItem:

					// Localize the delete button and set image
					var deleteButton = (ImageButton)e.Item.FindControl ("buttonDeleteSortOrder");
					deleteButton.ToolTip = deleteButton.AlternateText = LocalizeString ("buttonDeleteSortOrder.Text");
					deleteButton.ImageUrl = IconController.IconURL ("Delete");

					break;
			}
		}

		protected void grdDisplayColumns_ItemCreated (System.Object sender, System.Web.UI.WebControls.DataGridItemEventArgs e)
		{
			System.Web.UI.WebControls.ImageButton objUpImage = default(System.Web.UI.WebControls.ImageButton);
			System.Web.UI.WebControls.ImageButton objDownImage = default(System.Web.UI.WebControls.ImageButton);

			switch (e.Item.ItemType)
			{
				case System.Web.UI.WebControls.ListItemType.AlternatingItem:
				case System.Web.UI.WebControls.ListItemType.Item:
				case System.Web.UI.WebControls.ListItemType.SelectedItem:

					// Center the "visible" checkbox in its cell
					e.Item.Cells [1].Style.Add ("text-align", "center");

					// imgUp
					objUpImage = (System.Web.UI.WebControls.ImageButton)e.Item.Cells [2].FindControl ("imgUp");
					objUpImage.Visible = (e.Item.ItemIndex != 0);
					objUpImage.ImageUrl = IconController.IconURL ("Up", "16X16");
				
					// imgDown
					objDownImage = (System.Web.UI.WebControls.ImageButton)e.Item.Cells [2].FindControl ("imgDown");
					objDownImage.ImageUrl = IconController.IconURL ("Dn", "16X16");
					if (objUpImage.Visible == false)
					{
						objDownImage.Style.Add ("margin-left", "19px");
					}

					e.Item.CssClass = "Normal";

					break;
				case System.Web.UI.WebControls.ListItemType.Header:
					e.Item.CssClass = "SubHead";
					break;
			}
		}

		protected void grdDisplayColumns_ItemCommand (System.Object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
		{
			switch (e.CommandName)
			{
				case "DisplayOrderDown":
					// swap e.CommandArgument and the one after it
					SwapColumn (e.CommandArgument.ToString (), System.ComponentModel.ListSortDirection.Descending);
					break;
				case "DisplayOrderUp":
					// swap e.CommandArgument and the one before it
					SwapColumn (e.CommandArgument.ToString (), System.ComponentModel.ListSortDirection.Ascending);
					break;
			}
		}

		protected void lnkAddSortColumn_Click (System.Object sender, System.EventArgs e)
		{
			ArrayList objSortColumns = default(ArrayList);
			DocumentsSortColumnInfo objNewSortColumn = new DocumentsSortColumnInfo ();

			objSortColumns = RetrieveSortColumnSettings ();
			objNewSortColumn.ColumnName = comboSortFields.SelectedValue;
			objNewSortColumn.LocalizedColumnName = LocalizeString (objNewSortColumn.ColumnName + ".Header");
			if (comboSortOrderDirection.SelectedValue == "ASC")
			{
				objNewSortColumn.Direction = DocumentsSortColumnInfo.SortDirection.Ascending;
			}
			else
			{
				objNewSortColumn.Direction = DocumentsSortColumnInfo.SortDirection.Descending;
			}

			objSortColumns.Add (objNewSortColumn);
			BindSortSettings (objSortColumns);
		}

		protected void grdSortColumns_DeleteCommand (object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
		{
			ArrayList objSortColumns = default(ArrayList);
			DocumentsSortColumnInfo objSortColumnToDelete = new DocumentsSortColumnInfo ();

			objSortColumns = RetrieveSortColumnSettings ();

			foreach (DocumentsSortColumnInfo objSortColumnToDelete_loopVariable in objSortColumns)
			{
				objSortColumnToDelete = objSortColumnToDelete_loopVariable;
				if (objSortColumnToDelete.ColumnName == grdSortColumns.DataKeys [e.Item.ItemIndex].ToString ())
				{
					objSortColumns.Remove (objSortColumnToDelete);
					break; // TODO: might not be correct. Was : Exit For
				}
			}

			BindSortSettings (objSortColumns);
		}

		#endregion

		#region Control Handling/Utility Functions

		private void BindSortSettings (ArrayList objSortColumns)
		{
			SaveSortColumnSettings (objSortColumns);
			grdSortColumns.DataSource = objSortColumns;
			grdSortColumns.DataKeyField = "ColumnName";

			// REVIEW: Original:			Localization.LocalizeDataGrid(ref grdSortColumns, this.LocalResourceFile);
			Localization.LocalizeDataGrid (ref grdSortColumns, this.LocalResourceFile);
			grdSortColumns.DataBind ();
		}

		private void BindColumnSettings (List<DocumentsDisplayColumnInfo> objColumnSettings)
		{
			objColumnSettings.Sort ();
			SaveDisplayColumnSettings (objColumnSettings);
			grdDisplayColumns.DataSource = objColumnSettings;
			grdDisplayColumns.DataKeyField = "ColumnName";

			if (!this.IsPostBack)
			{
				
				// REVIEW: Original: Localization.LocalizeDataGrid(ref grdDisplayColumns, this.LocalResourceFile);
				Localization.LocalizeDataGrid (ref grdSortColumns, this.LocalResourceFile);
			}
 
			grdDisplayColumns.DataBind ();

			var _with6 = (System.Web.UI.WebControls.ImageButton)grdDisplayColumns.Items [grdDisplayColumns.Items.Count - 1].Cells [2].FindControl ("imgDown");
			// Set down arrow invisible on the last item
			_with6.Visible = false;

		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// Read settings from the screen into the passed-in DocumentsSettings object
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <history>
		/// </history>
		/// -----------------------------------------------------------------------------
		private void FillSettings ()
		{
			string strDisplayColumns = "";
			DocumentsDisplayColumnInfo objColumnInfo = null;
			int intIndex = 0;
			ArrayList objSortColumns = default(ArrayList);
			string strSortColumnList = "";
			DocumentsSortColumnInfo objSortColumn = null;

			//Ensure that if categories list is checked that we did have an available category
			if ((chkUseCategoriesList.Checked && !lstNoListsAvailable.Visible))
			{
				//If so, set normally
				Settings.UseCategoriesList = chkUseCategoriesList.Checked;
				Settings.CategoriesListName = cboCategoriesList.SelectedValue;
			}
			else
			{
				//Otherwise default values
				Settings.UseCategoriesList = false;
				Settings.CategoriesListName = "";
			}

			Settings.ShowTitleLink = chkShowTitleLink.Checked;
			Settings.AllowUserSort = chkAllowUserSort.Checked;
            Settings.GridStyle = comboGridStyle.SelectedItem.Value;

			if (folderDefaultFolder.SelectedFolder != null)
				Settings.DefaultFolder = folderDefaultFolder.SelectedFolder.FolderID;
			else
				Settings.DefaultFolder = null;

			var objColumnSettings = RetrieveDisplayColumnSettings ();
			intIndex = 0;
			foreach (DocumentsDisplayColumnInfo objColumnInfo_loopVariable in objColumnSettings)
			{
				objColumnInfo = objColumnInfo_loopVariable;
				// Figure out column visibility
				objColumnInfo.Visible = ((System.Web.UI.WebControls.CheckBox)grdDisplayColumns.Items [intIndex].Cells [1].FindControl ("chkVisible")).Checked;

				if (strDisplayColumns != string.Empty)
				{
					strDisplayColumns = strDisplayColumns + ",";
				}
				strDisplayColumns = strDisplayColumns + objColumnInfo.ColumnName + ";" + objColumnInfo.Visible.ToString ();

				intIndex = intIndex + 1;
			}

			Settings.DisplayColumns = strDisplayColumns;

			objSortColumns = RetrieveSortColumnSettings ();
			foreach (DocumentsSortColumnInfo objSortColumn_loopVariable in objSortColumns)
			{
				objSortColumn = objSortColumn_loopVariable;
				if (strSortColumnList != string.Empty)
				{
					strSortColumnList = strSortColumnList + ",";
				}
				strSortColumnList = strSortColumnList + (objSortColumn.Direction == DocumentsSortColumnInfo.SortDirection.Descending ? "-" : "").ToString () + objSortColumn.ColumnName;
			}
			Settings.SortOrder = strSortColumnList;
            Settings.GridStyle = comboGridStyle.SelectedValue;
		}

		private void SwapColumn (string ColumnName, System.ComponentModel.ListSortDirection Direction)
		{
			int intIndex = 0;
			int intDisplayOrderTemp = 0;

			// First, find the column we want
			var objColumnSettings = RetrieveDisplayColumnSettings ();
			intIndex = DocumentsSettings.FindColumn (ColumnName, objColumnSettings, false);

			// Swap display orders
			if (intIndex >= 0)
			{
				switch (Direction)
				{
					case System.ComponentModel.ListSortDirection.Ascending:
						// swap up
						if (intIndex > 0)
						{
							intDisplayOrderTemp = ((DocumentsDisplayColumnInfo)objColumnSettings [intIndex]).DisplayOrder;
							((DocumentsDisplayColumnInfo)objColumnSettings [intIndex]).DisplayOrder = ((DocumentsDisplayColumnInfo)objColumnSettings [intIndex - 1]).DisplayOrder;
							((DocumentsDisplayColumnInfo)objColumnSettings [intIndex - 1]).DisplayOrder = intDisplayOrderTemp;
						}
						break;
					case System.ComponentModel.ListSortDirection.Descending:
						// swap down
						if (intIndex < objColumnSettings.Count)
						{
							intDisplayOrderTemp = ((DocumentsDisplayColumnInfo)objColumnSettings [intIndex]).DisplayOrder;
							((DocumentsDisplayColumnInfo)objColumnSettings [intIndex]).DisplayOrder = ((DocumentsDisplayColumnInfo)objColumnSettings [intIndex + 1]).DisplayOrder;
							((DocumentsDisplayColumnInfo)objColumnSettings [intIndex + 1]).DisplayOrder = intDisplayOrderTemp;
						}
						break;
				}
			}

			// Re-bind the newly sorted collection to the datagrid
			BindColumnSettings (objColumnSettings);
		}

		#endregion

		private void SaveSortColumnSettings (ArrayList objSettings)
		{
			// Custom viewstate implementation to avoid reflection
			DocumentsSortColumnInfo objSortColumnInfo = null;
			string strValues = "";

			foreach (DocumentsSortColumnInfo objSortColumnInfo_loopVariable in objSettings)
			{
				objSortColumnInfo = objSortColumnInfo_loopVariable;
				if (strValues != string.Empty)
				{
					strValues = strValues + "#";
				}

				strValues = strValues + objSortColumnInfo.ColumnName + "," + objSortColumnInfo.LocalizedColumnName + "," + objSortColumnInfo.Direction.ToString ();
			}
			ViewState [VIEWSTATE_SORTCOLUMNSETTINGS] = strValues;
		}

		private ArrayList RetrieveSortColumnSettings ()
		{
			// Custom viewstate implementation to avoid reflection
			ArrayList objSortColumnSettings = new ArrayList ();
			DocumentsSortColumnInfo objSortColumnInfo = null;

			string strValues = null;

			strValues = Convert.ToString (ViewState [VIEWSTATE_SORTCOLUMNSETTINGS]);
			if ((strValues != null) && strValues != string.Empty)
			{
				foreach (string strSortColumnSetting in strValues.Split(char.Parse("#")))
				{
					objSortColumnInfo = new DocumentsSortColumnInfo ();
					objSortColumnInfo.ColumnName = strSortColumnSetting.Split (char.Parse (",")) [0];
					objSortColumnInfo.LocalizedColumnName = strSortColumnSetting.Split (char.Parse (",")) [1];
					objSortColumnInfo.Direction = (DocumentsSortColumnInfo.SortDirection)System.Enum.Parse (typeof(DocumentsSortColumnInfo.SortDirection), strSortColumnSetting.Split (char.Parse (",")) [2]);

					objSortColumnSettings.Add (objSortColumnInfo);
				}
			}

			return objSortColumnSettings;
		}

		private void SaveDisplayColumnSettings (List<DocumentsDisplayColumnInfo> objSettings)
		{
			// Custom viewstate implementation to avoid reflection
			DocumentsDisplayColumnInfo objDisplayColumnInfo = null;
			string strValues = "";

			foreach (DocumentsDisplayColumnInfo objDisplayColumnInfo_loopVariable in objSettings)
			{
				objDisplayColumnInfo = objDisplayColumnInfo_loopVariable;
				if (strValues != string.Empty)
				{
					strValues = strValues + "#";
				}
				strValues = strValues + objDisplayColumnInfo.ColumnName + "," + objDisplayColumnInfo.LocalizedColumnName + "," + objDisplayColumnInfo.DisplayOrder + "," + objDisplayColumnInfo.Visible;
			}
			ViewState [VIEWSTATE_DISPLAYCOLUMNSETTINGS] = strValues;
		}

		private List<DocumentsDisplayColumnInfo> RetrieveDisplayColumnSettings ()
		{
			// Custom viewstate implementation to avoid reflection
			var objDisplayColumnSettings = new List<DocumentsDisplayColumnInfo> ();
			DocumentsDisplayColumnInfo objDisplayColumnInfo = null;

			string strValues = null;

			strValues = Convert.ToString (ViewState [VIEWSTATE_DISPLAYCOLUMNSETTINGS]);
			if (!string.IsNullOrEmpty (strValues))
			{
				foreach (string strDisplayColumnSetting in strValues.Split('#'))
				{
					objDisplayColumnInfo = new DocumentsDisplayColumnInfo ();
					objDisplayColumnInfo.ColumnName = strDisplayColumnSetting.Split (char.Parse (",")) [0];
					objDisplayColumnInfo.LocalizedColumnName = strDisplayColumnSetting.Split (char.Parse (",")) [1];
					objDisplayColumnInfo.DisplayOrder = Convert.ToInt32 (strDisplayColumnSetting.Split (char.Parse (",")) [2]);
					objDisplayColumnInfo.Visible = Convert.ToBoolean (strDisplayColumnSetting.Split (char.Parse (",")) [3]);

					objDisplayColumnSettings.Add (objDisplayColumnInfo);
				}
			}

			return objDisplayColumnSettings;
		}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			if (UserInfo.IsSuperUser)
			{
				lnkEditLists.Text = Localization.GetString ("lnkEditLists", base.LocalResourceFile);

				//lnkEditLists.Target = "_blank"

				try
				{
					var _with7 = new TabController ();
					lnkEditLists.NavigateUrl = _with7.GetTabByName ("Lists", Null.NullInteger).FullUrl;
				}
				catch
				{
					//Unable to locate "Lists" tab
					lblCannotEditLists.Text = Localization.GetString ("UnableToFindLists", base.LocalResourceFile);
					lblCannotEditLists.Visible = true;
					lnkEditLists.Visible = false;
				}
			}
			else
			{
				//Show error, then hide the "Edit" link
				lblCannotEditLists.Text = Localization.GetString ("NoListAccess", base.LocalResourceFile);
				lblCannotEditLists.Visible = true;
				lnkEditLists.Visible = false;
			}
		}
	}
}
