﻿//
// DocumentsController.cs
//
// Author:
//       Roman M. Yagodin <roman.yagodin@gmail.com>
//
// Copyright (c) 2014 
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
using System.Text;
using System.Xml;
using System.Linq;
using DotNetNuke.Collections;
using DotNetNuke.Data;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Search;
using DotNetNuke.Services.Search.Entities;


namespace R7.Documents
{
	public partial class DocumentsController : ControllerBase, ISearchable, IPortable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Documents.DocumentsController"/> class.
		/// </summary>
		public DocumentsController () : base ()
		{ 

		}

		public DocumentInfo GetDocument (int ItemId, int ModuleId)
		{
			DocumentInfo document;

			using (var ctx = DataContext.Instance ())
			{
				document = ctx.ExecuteSingleOrDefault<DocumentInfo> (
					System.Data.CommandType.StoredProcedure, "Documents_GetDocument", ItemId, ModuleId);
			}

			return document;
		}

		public IEnumerable<DocumentInfo> GetDocuments (int ModuleId, int PortalId)
		{
			IEnumerable<DocumentInfo> documents;

			using (var ctx = DataContext.Instance ())
			{
				documents = ctx.ExecuteQuery<DocumentInfo> (
					System.Data.CommandType.StoredProcedure, "Documents_GetDocuments", ModuleId, PortalId);
			}

			return documents;
		}

		#region ModuleSearchBase implementaion

		public override IList<SearchDocument> GetModifiedSearchDocuments (ModuleInfo modInfo, DateTime beginDate)
		{
			var searchDocs = new List<SearchDocument> ();

			// TODO: Realize GetModifiedSearchDocuments()

			/* var sd = new SearchDocument();
			searchDocs.Add(searchDoc);
			*/

			return searchDocs;
		}

		#endregion

		#region "Public Methods"


		/*
		public void AddDocument(DocumentInfo objDocument)
		{
			DataProvider.Instance().AddDocument(objDocument.ModuleId, objDocument.Title, objDocument.Url, objDocument.CreatedByUserId, objDocument.OwnedByUserId, objDocument.Category, objDocument.SortOrderIndex, objDocument.Description, objDocument.ForceDownload);

		}


		public void DeleteDocument(int ModuleId, int ItemID)
		{
			DataProvider.Instance().DeleteDocument(ModuleId, ItemID);

		}

		public DocumentInfo GetDocument(int ItemId, int ModuleId)
		{

			return (DocumentInfo)CBO.FillObject(DataProvider.Instance().GetDocument(ItemId, ModuleId), typeof(DocumentInfo));

		}

		public ArrayList GetDocuments(int ModuleId, int PortalId)
		{

			return CBO.FillCollection(DataProvider.Instance().GetDocuments(ModuleId, PortalId), typeof(DocumentInfo));

		}

		public void UpdateDocument(DocumentInfo objDocument)
		{
			DataProvider.Instance().UpdateDocument(objDocument.ModuleId, objDocument.ItemId, objDocument.Title, objDocument.Url, objDocument.CreatedByUserId, objDocument.OwnedByUserId, objDocument.Category, objDocument.SortOrderIndex, objDocument.Description, objDocument.ForceDownload);
		}
*/

		#endregion

		#region "Optional Interfaces"

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// GetSearchItems implements the ISearchable Interface
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="ModInfo">The ModuleInfo for the module to be Indexed</param>
		/// <history>
		///		[cnurse]	    17 Nov 2004	documented
		///   [aglenwright] 18 Feb 2006 Altered to accomodate change to CreatedByUser
		///                             field (changed from string to integer)
		/// </history>
		/// -----------------------------------------------------------------------------
		public DotNetNuke.Services.Search.SearchItemInfoCollection GetSearchItems (ModuleInfo ModInfo)
		{
			SearchItemInfoCollection SearchItemCollection = new SearchItemInfoCollection ();
			// ArrayList Documents = GetDocuments(ModInfo.ModuleID, ModInfo.PortalID);
			var documents = GetObjects<DocumentInfo> (ModInfo.ModuleID); // PortalID!

			// TODO: Add new fields

			object objDocument = null;
			foreach (object objDocument_loopVariable in documents)
			{
				objDocument = objDocument_loopVariable;
				SearchItemInfo SearchItem = default(SearchItemInfo);
				var _with1 = (DocumentInfo)objDocument;
				int UserId = Null.NullInteger;
				//If IsNumeric(.CreatedByUser) Then
				//    UserId = Integer.Parse(.CreatedByUser)
				//End If
				UserId = _with1.CreatedByUserId;
				SearchItem = new SearchItemInfo (
					ModInfo.ModuleTitle + " - " + _with1.Title, 
					_with1.Title, 
					UserId, 
					_with1.CreatedDate, 
					ModInfo.ModuleID, 
					_with1.ItemId.ToString (), 
					_with1.Title + " " + _with1.Category + " " + _with1.Description, 
					"ItemId=" + _with1.ItemId);
				SearchItemCollection.Add (SearchItem);
			}

			return SearchItemCollection;
		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// ExportModule implements the IPortable ExportModule Interface
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="ModuleID">The Id of the module to be exported</param>
		/// <history>
		///		[cnurse]	    17 Nov 2004	documented
		///		[aglenwright]	18 Feb 2006	Added new fields: Createddate, description, 
		///                             modifiedbyuser, modifieddate, OwnedbyUser, SortorderIndex
		///                             Added DocumentsSettings
		///   [togrean]     10 Jul 2007 Fixed issues with importing documet settings since new fileds 
		///                             were added: AllowSorting, default folder, list name
		///   [togrean]     13 Jul 2007 Added support for exporting documents Url tracking options  
		/// </history>
		/// -----------------------------------------------------------------------------
		public string ExportModule (int ModuleID)
		{

			ModuleController objModules = new ModuleController ();
			ModuleInfo objModule = objModules.GetModule (ModuleID, Null.NullInteger);
		
			StringBuilder strXML = new StringBuilder ("<documents>");
		
			try
			{
				var arrDocuments = GetDocuments (ModuleID, objModule.PortalID);
				
				if (arrDocuments.Any ())
				{
					foreach (DocumentInfo objDocument_loopVariable in arrDocuments)
					{
						var objDocument = objDocument_loopVariable;
						strXML.Append ("<document>");
						strXML.AppendFormat ("<title>{0}</title>", XmlUtils.XMLEncode (objDocument.Title));
						strXML.AppendFormat ("<url>{0}</url>", XmlUtils.XMLEncode (objDocument.Url));
						strXML.AppendFormat ("<category>{0}</category>", XmlUtils.XMLEncode (objDocument.Category));

						strXML.AppendFormat ("<createddate>{0}</createddate>", XmlUtils.XMLEncode (objDocument.CreatedDate.ToString ("dd-MMM-yyyy hh:mm:ss tt")));
						strXML.AppendFormat ("<description>{0}</description>", XmlUtils.XMLEncode (objDocument.Description));
						strXML.AppendFormat ("<createdbyuserid>{0}</createdbyuserid>", XmlUtils.XMLEncode (objDocument.CreatedByUserId.ToString ()));
						strXML.AppendFormat ("<forcedownload>{0}</forcedownload>", XmlUtils.XMLEncode ((objDocument.ForceDownload.ToString ())));
						strXML.AppendFormat ("<ispublished>{0}</ispublished>", XmlUtils.XMLEncode ((objDocument.IsPublished.ToString ())));
						strXML.AppendFormat ("<ownedbyuserid>{0}</ownedbyuserid>", XmlUtils.XMLEncode (objDocument.OwnedByUserId.ToString ()));
						strXML.AppendFormat ("<modifiedbyuserid>{0}</modifiedbyuserid>", XmlUtils.XMLEncode (objDocument.ModifiedByUserId.ToString ()));
						strXML.AppendFormat ("<modifieddate>{0}</modifieddate>", XmlUtils.XMLEncode (objDocument.ModifiedDate.ToString ("dd-MMM-yyyy hh:mm:ss tt")));
						strXML.AppendFormat ("<sortorderindex>{0}</sortorderindex>", XmlUtils.XMLEncode (objDocument.SortOrderIndex.ToString ()));

						// Export Url Tracking options too
						UrlController objUrlController = new UrlController ();
						UrlTrackingInfo objUrlTrackingInfo = objUrlController.GetUrlTracking (objModule.PortalID, objDocument.Url, ModuleID);

						if ((objUrlTrackingInfo != null))
						{
							strXML.AppendFormat ("<logactivity>{0}</logactivity>", XmlUtils.XMLEncode (objUrlTrackingInfo.LogActivity.ToString ()));
							strXML.AppendFormat ("<trackclicks>{0}</trackclicks>", XmlUtils.XMLEncode (objUrlTrackingInfo.TrackClicks.ToString ()));
							strXML.AppendFormat ("<newwindow>{0}</newwindow>", XmlUtils.XMLEncode (objUrlTrackingInfo.NewWindow.ToString ()));
						}
						strXML.Append ("</document>");
					}
				}

				var settings = new DocumentsSettings (objModule);
				strXML.Append ("<settings>");
				strXML.AppendFormat ("<allowusersort>{0}</allowusersort>", XmlUtils.XMLEncode (settings.AllowUserSort.ToString ()));
				strXML.AppendFormat ("<showtitlelink>{0}</showtitlelink>", XmlUtils.XMLEncode (settings.ShowTitleLink.ToString ()));
				strXML.AppendFormat ("<usecategorieslist>{0}</usecategorieslist>", XmlUtils.XMLEncode (settings.UseCategoriesList.ToString ()));
				strXML.AppendFormat ("<categorieslistname>{0}</categorieslistname>", XmlUtils.XMLEncode (settings.CategoriesListName));
				strXML.AppendFormat ("<defaultfolder>{0}</defaultfolder>", XmlUtils.XMLEncode (settings.DefaultFolder));
				strXML.AppendFormat ("<displaycolumns>{0}</displaycolumns>", XmlUtils.XMLEncode (settings.DisplayColumns));
				strXML.AppendFormat ("<sortorder>{0}</sortorder>", XmlUtils.XMLEncode (settings.SortOrder));
				strXML.Append ("</settings>");
			}
			catch
			{
				// Catch errors but make sure XML is valid
			}
			finally
			{
				strXML.Append ("</documents>");
			}

			return strXML.ToString ();

		}

		/// -----------------------------------------------------------------------------
		/// <summary>
		/// ImportModule implements the IPortable ImportModule Interface
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="ModuleID">The Id of the module to be imported</param>
		/// <history>
		///		[cnurse]	    17 Nov 2004	documented
		///		[aglenwright]	18 Feb 2006	Added new fields: Createddate, description, 
		///                             modifiedbyuser, modifieddate, OwnedbyUser, SortorderIndex
		///                             Added DocumentsSettings
		///   [togrean]     10 Jul 2007 Fixed issues with importing documet settings since new fileds 
		///                             were added: AllowSorting, default folder, list name    
		///   [togrean]     13 Jul 2007 Added support for importing documents Url tracking options     
		/// </history>
		/// -----------------------------------------------------------------------------
		public void ImportModule (int ModuleID, string Content, string Version, int UserId)
		{
			ModuleController objModules = new ModuleController ();
			ModuleInfo objModule = objModules.GetModule (ModuleID, Null.NullInteger);
			
			// XmlNode xmlDocument = default(XmlNode);
			string strUrl = string.Empty;
			XmlNode xmlDocuments = Globals.GetContent (Content, "documents");
			XmlNodeList documentNodes = xmlDocuments.SelectNodes ("document");
			foreach (XmlNode xmlDocument in documentNodes)
			{
				DocumentInfo objDocument = new DocumentInfo ();
				objDocument.ModuleId = ModuleID;
				objDocument.Title = xmlDocument ["title"].InnerText;
				strUrl = xmlDocument ["url"].InnerText;
				if ((strUrl.ToLower ().StartsWith ("fileid=")))
				{
					objDocument.Url = strUrl;
				}
				else
				{
					objDocument.Url = Globals.ImportUrl (ModuleID, strUrl);
				}

				objDocument.Category = xmlDocument ["category"].InnerText;
				objDocument.CreatedDate = XmlUtils.GetNodeValueDate (xmlDocument, "createddate", DateTime.Now);
				objDocument.Description = XmlUtils.GetNodeValue (xmlDocument, "description");
				objDocument.CreatedByUserId = UserId;
				objDocument.OwnedByUserId = XmlUtils.GetNodeValueInt (xmlDocument, "ownedbyuserid");
				objDocument.ModifiedByUserId = XmlUtils.GetNodeValueInt (xmlDocument, "modifiedbyuserid");
				objDocument.ModifiedDate = XmlUtils.GetNodeValueDate (xmlDocument, "modifieddate", DateTime.Now);
				objDocument.SortOrderIndex = XmlUtils.GetNodeValueInt (xmlDocument, "sortorderindex");
				objDocument.ForceDownload = XmlUtils.GetNodeValueBoolean (xmlDocument, "forcedownload");
				objDocument.IsPublished = XmlUtils.GetNodeValueBoolean (xmlDocument, "ispublished");

				Add<DocumentInfo> (objDocument);

				// Update Tracking options
				string urlType = "U";
				if (objDocument.Url.StartsWith ("FileID=", StringComparison.InvariantCultureIgnoreCase))
				{
					urlType = "F";
				}
				UrlController urlController = new UrlController ();
				// If nodes not found, all values will be false
				urlController.UpdateUrl (objModule.PortalID, objDocument.Url, urlType, XmlUtils.GetNodeValueBoolean (xmlDocument, "logactivity"), XmlUtils.GetNodeValueBoolean (xmlDocument, "trackclicks", true), ModuleID, XmlUtils.GetNodeValueBoolean (xmlDocument, "newwindow"));
			}

			XmlNode xmlDocumentsSettings = Globals.GetContent (Content, "documents/settings");
			if (xmlDocumentsSettings != null)
			{
				var settings = new DocumentsSettings (objModule);
			
				settings.AllowUserSort = XmlUtils.GetNodeValueBoolean (xmlDocumentsSettings, "allowusersort");
				settings.ShowTitleLink = XmlUtils.GetNodeValueBoolean (xmlDocumentsSettings, "showtitlelink");
				settings.UseCategoriesList = XmlUtils.GetNodeValueBoolean (xmlDocumentsSettings, "usecategorieslist");
				settings.CategoriesListName = XmlUtils.GetNodeValue (xmlDocumentsSettings, "categorieslistname");
				settings.DefaultFolder = XmlUtils.GetNodeValue (xmlDocumentsSettings, "defaultfolder");
				settings.DisplayColumns = XmlUtils.GetNodeValue (xmlDocumentsSettings, "displaycolumns");
				settings.SortOrder = XmlUtils.GetNodeValue (xmlDocumentsSettings, "sortorder");

				// Need Utils.SynchronizeModule() call
			}

		}

		#endregion
	}
}

