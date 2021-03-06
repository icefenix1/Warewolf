﻿using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Warewolf.UITests.SaveDialog
{
    [CodedUITest]
    public class SaveDialogTests
    {
        private const string HelloWorld = "Hello World";
        private const string FolderToRename = "FolderToRename";
        private const string FolderRenamed = "FolderToRename_Renamed";

        [TestMethod]
        [TestCategory("Save Dialog")]
        public void Save_Dialog_Filter_Given_HelloWorld_Filters_Explorer_Tree()
        {
            UIMap.Filter_Save_Dialog_Explorer(HelloWorld);
            Assert.IsTrue(UIMap.ControlExistsNow(UIMap.SaveDialogWindow.ExplorerView.ExplorerTree.localhost.FirstItem));
            Assert.IsFalse(UIMap.ControlExistsNow(UIMap.SaveDialogWindow.ExplorerView.ExplorerTree.localhost.SecondItem));
            UIMap.Click_SaveDialog_CancelButton();
            UIMap.Click_Close_Workflow_Tab_Button();
            UIMap.Click_MessageBox_No();
        }
        
        [TestMethod]
        [TestCategory("Save Dialog")]
        public void Server_Context_Menu_Has_New_Folder_Only()
        {
            UIMap.RightClick_Save_Dialog_Localhost();
            Assert.IsTrue(UIMap.SaveDialogWindow.SaveDialogContextMenu.NewFolderMenuItem.Exists);
            UIMap.Click_SaveDialog_CancelButton();
            UIMap.Click_Close_Workflow_Tab_Button();
            UIMap.Click_MessageBox_No();
        }

        [TestMethod]
        [TestCategory("Save Dialog")]
        public void Folder_Items_Context_Menu_Has_New_Folder_And_Rename()
        {
            UIMap.Filter_Save_Dialog_Explorer(FolderToRename);
            UIMap.RightClick_Save_Dialog_Localhost_First_Item();
            Assert.IsTrue(UIMap.SaveDialogWindow.SaveDialogContextMenu.RenameMenuItem.Exists);
            Assert.IsTrue(UIMap.SaveDialogWindow.SaveDialogContextMenu.UINewFolderMenuItem.Exists);
            UIMap.Click_SaveDialog_CancelButton();
            UIMap.Click_Close_Workflow_Tab_Button();
            UIMap.Click_MessageBox_No();
        }

        [TestMethod]
        [TestCategory("Save Dialog")]
        public void Resources_Items_Context_Menu_Has_Delete_And_Rename()
        {            
            UIMap.Filter_Save_Dialog_Explorer(HelloWorld);
            UIMap.RightClick_Save_Dialog_Localhost_First_Item();
            Assert.IsTrue(UIMap.SaveDialogWindow.SaveDialogContextMenu.DeleteMenuItem.Exists);
            Assert.IsTrue(UIMap.SaveDialogWindow.SaveDialogContextMenu.RenameMenuItem.Exists);
            UIMap.Click_SaveDialog_CancelButton();
            UIMap.Click_Close_Workflow_Tab_Button();
            UIMap.Click_MessageBox_No();
        }

        #region Additional test attributes

        [TestInitialize]
        public void MyTestInitialize()
        {
            UIMap.SetPlaybackSettings();
            UIMap.AssertStudioIsRunning();
            UIMap.Click_New_Workflow_Ribbon_Button();
            UIMap.Drag_Toolbox_MultiAssign_Onto_DesignSurface();
            UIMap.Click_Save_Ribbon_Button_to_Open_Save_Dialog();
        }

        UIMap UIMap
        {
            get
            {
                if (_UIMap == null)
                {
                    _UIMap = new UIMap();
                }

                return _UIMap;
            }
        }

        private UIMap _UIMap;

        #endregion
    }
}
