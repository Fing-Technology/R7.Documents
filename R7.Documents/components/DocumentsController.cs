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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using DotNetNuke.Collections;
using DotNetNuke.Data;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Search;
using DotNetNuke.Services.Search.Entities;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.R7;
using R7.Documents.Data;

namespace R7.Documents
{
	public partial class DocumentsController : ModuleSearchBase, IPortable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Documents.DocumentsController"/> class.
		/// </summary>
		public DocumentsController () : base ()
		{ 
		}

		#region ModuleSearchBase implementaion

		public override IList<SearchDocument> GetModifiedSearchDocuments (ModuleInfo moduleInfo, DateTime beginDate)
		{
			var searchDocs = new List<SearchDocument> ();
			
            var documents = DocumentsDataProvider.Instance.GetDocuments (moduleInfo.ModuleID, moduleInfo.PortalID);

            foreach (var document in documents ?? Enumerable.Empty<DocumentInfo> ())
			{
				if (document.ModifiedDate.ToUniversalTime () > beginDate.ToUniversalTime ())
				{
                    var documentText = TextUtils.FormatList (" ", document.Title, document.Description);

					var sd = new SearchDocument () {
						PortalId = moduleInfo.PortalID,
                        AuthorUserId = document.ModifiedByUserId,
						Title = document.Title,
						Description = HtmlUtils.Shorten (documentText, 255, "..."),
						Body = documentText,
						ModifiedTimeUtc = document.ModifiedDate.ToUniversalTime (),
						UniqueKey = string.Format ("Documents_Document_{0}", document.ItemId),
						IsActive = document.IsPublished,
                        Url = string.Format ("/Default.aspx?tabid={0}#{1}", moduleInfo.TabID, moduleInfo.ModuleID)

                        // FIXME: This one produce null reference exception
                        // Url = Globals.LinkClick (document.Url, moduleInfo.TabID, moduleInfo.ModuleID, document.TrackClicks, document.ForceDownload)
					};
					
					searchDocs.Add (sd);
				}
			}
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

		#region IPortable implementation

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
		public string ExportModule (int moduleId)
		{
            var mCtrl = new ModuleController ();
            var module = mCtrl.GetModule (moduleId, Null.NullInteger);
		
            var strXml = new StringBuilder ("<documents>");
		
			try
			{
                var documents = DocumentsDataProvider.Instance.GetDocuments (moduleId, module.PortalID);
				
				if (documents.Any ())
				{
                    foreach (var document in documents)
					{
						strXml.Append ("<document>");
						strXml.AppendFormat ("<title>{0}</title>", XmlUtils.XMLEncode (document.Title));
                        strXml.AppendFormat ("<url>{0}</url>", XmlUtils.XMLEncode (document.Url));
                        strXml.AppendFormat ("<category>{0}</category>", XmlUtils.XMLEncode (document.Category));
                        strXml.AppendFormat ("<description>{0}</description>", XmlUtils.XMLEncode (document.Description));
                        strXml.AppendFormat ("<forcedownload>{0}</forcedownload>", XmlUtils.XMLEncode (document.ForceDownload.ToString ()));
						strXml.AppendFormat ("<ispublished>{0}</ispublished>", XmlUtils.XMLEncode (document.IsPublished.ToString ()));
						strXml.AppendFormat ("<ownedbyuserid>{0}</ownedbyuserid>", XmlUtils.XMLEncode (document.OwnedByUserId.ToString ()));
						strXml.AppendFormat ("<sortorderindex>{0}</sortorderindex>", XmlUtils.XMLEncode (document.SortOrderIndex.ToString ()));
                        strXml.AppendFormat ("<linkattributes>{0}</linkattributes>", XmlUtils.XMLEncode (document.LinkAttributes));

						// Export Url Tracking options too
						var urlCtrl = new UrlController ();
                        var urlTrackingInfo = urlCtrl.GetUrlTracking (module.PortalID, document.Url, moduleId);

						if ((urlTrackingInfo != null))
						{
							strXml.AppendFormat ("<logactivity>{0}</logactivity>", XmlUtils.XMLEncode (urlTrackingInfo.LogActivity.ToString ()));
							strXml.AppendFormat ("<trackclicks>{0}</trackclicks>", XmlUtils.XMLEncode (urlTrackingInfo.TrackClicks.ToString ()));
							strXml.AppendFormat ("<newwindow>{0}</newwindow>", XmlUtils.XMLEncode (urlTrackingInfo.NewWindow.ToString ()));
						}
						strXml.Append ("</document>");
					}
				}

				var settings = new DocumentsSettings (module);
				strXml.Append ("<settings>");
				strXml.AppendFormat ("<allowusersort>{0}</allowusersort>", XmlUtils.XMLEncode (settings.AllowUserSort.ToString ()));
				strXml.AppendFormat ("<showtitlelink>{0}</showtitlelink>", XmlUtils.XMLEncode (settings.ShowTitleLink.ToString ()));
				strXml.AppendFormat ("<usecategorieslist>{0}</usecategorieslist>", XmlUtils.XMLEncode (settings.UseCategoriesList.ToString ()));
				strXml.AppendFormat ("<categorieslistname>{0}</categorieslistname>", XmlUtils.XMLEncode (settings.CategoriesListName));
				strXml.AppendFormat ("<defaultfolder>{0}</defaultfolder>", XmlUtils.XMLEncode (settings.DefaultFolder.ToString()));
				strXml.AppendFormat ("<displaycolumns>{0}</displaycolumns>", XmlUtils.XMLEncode (settings.DisplayColumns));
				strXml.AppendFormat ("<sortorder>{0}</sortorder>", XmlUtils.XMLEncode (settings.SortOrder));
				strXml.Append ("</settings>");
			}
			catch
			{
				// catch errors
			}
			finally
			{
                // make sure XML is valid
				strXml.Append ("</documents>");
			}

			return strXml.ToString ();
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
		public void ImportModule (int moduleId, string content, string version, int userId)
		{
            var mCtrl = new ModuleController ();
            var module = mCtrl.GetModule (moduleId, Null.NullInteger);
			
			var xmlDocuments = Globals.GetContent (content, "documents");
			var documentNodes = xmlDocuments.SelectNodes ("document");

            foreach (XmlNode documentNode in documentNodes)
			{
                var document = new DocumentInfo ();
				document.ModuleId = moduleId;
				document.Title = documentNode ["title"].InnerText;
				
                var strUrl = documentNode ["url"].InnerText;
                document.Url = strUrl.StartsWith ("fileid=", StringComparison.InvariantCultureIgnoreCase) ?
                    strUrl : Globals.ImportUrl (moduleId, strUrl);

				document.Category = documentNode ["category"].InnerText;
				document.Description = XmlUtils.GetNodeValue (documentNode, "description");
				document.OwnedByUserId = XmlUtils.GetNodeValueInt (documentNode, "ownedbyuserid");
				document.SortOrderIndex = XmlUtils.GetNodeValueInt (documentNode, "sortorderindex");
                document.LinkAttributes = XmlUtils.GetNodeValue (documentNode, "linkattributes");
                document.ForceDownload = XmlUtils.GetNodeValueBoolean (documentNode, "forcedownload");
                document.IsPublished = XmlUtils.GetNodeValueBoolean (documentNode, "ispublished");

				document.CreatedByUserId = userId;
				document.ModifiedByUserId = userId;
				
				var now = DateTime.Now;
				document.CreatedDate = now;
				document.ModifiedDate = now;

                DocumentsDataProvider.Instance.Add<DocumentInfo> (document);

				// Update Tracking options
                var urlType = document.Url.StartsWith ("fileid=", StringComparison.InvariantCultureIgnoreCase)? "F" : "U";

                var urlCtrl = new UrlController ();
				// If nodes not found, all values will be false
				urlCtrl.UpdateUrl (module.PortalID, document.Url, urlType, XmlUtils.GetNodeValueBoolean (documentNode, "logactivity"), XmlUtils.GetNodeValueBoolean (documentNode, "trackclicks", true), moduleId, XmlUtils.GetNodeValueBoolean (documentNode, "newwindow"));
			}

            var xmlSettings = Globals.GetContent (content, "documents/settings");
			if (xmlSettings != null)
			{
				var settings = new DocumentsSettings (module);
			
				settings.AllowUserSort = XmlUtils.GetNodeValueBoolean (xmlSettings, "allowusersort");
				settings.ShowTitleLink = XmlUtils.GetNodeValueBoolean (xmlSettings, "showtitlelink");
				settings.UseCategoriesList = XmlUtils.GetNodeValueBoolean (xmlSettings, "usecategorieslist");
				settings.CategoriesListName = XmlUtils.GetNodeValue (xmlSettings, "categorieslistname");
                settings.DefaultFolder = TypeUtils.ParseToNullable<int> (XmlUtils.GetNodeValue (xmlSettings, "defaultfolder"));
				settings.DisplayColumns = XmlUtils.GetNodeValue (xmlSettings, "displaycolumns");
				settings.SortOrder = XmlUtils.GetNodeValue (xmlSettings, "sortorder");

				// Need Utils.SynchronizeModule() call
			}
		}

		#endregion
	}
}

