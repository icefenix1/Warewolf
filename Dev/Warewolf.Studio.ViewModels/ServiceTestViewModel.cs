﻿
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Dev2;
using Dev2.Common.Common;
using Dev2.Common.Interfaces;
using Dev2.Common.Interfaces.Studio.Controller;
using Dev2.Interfaces;
using Dev2.Studio.Core.Interfaces;
using Dev2.Studio.Core.Network;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;

namespace Warewolf.Studio.ViewModels
{
    public class ServiceTestViewModel : BindableBase, IServiceTestViewModel
    {
        private IServiceTestModel _selectedServiceTest;
        private string _runAllTestsUrl;
        private string _testPassingResult;
        private ObservableCollection<IServiceTestModel> _tests;
        private string _displayName;
        public IPopupController PopupController { get; set; }

        public ServiceTestViewModel(IContextualResourceModel resourceModel)
        {
            if (resourceModel == null)
                throw new ArgumentNullException(nameof(resourceModel));
            ResourceModel = resourceModel;
            DisplayName = resourceModel.DisplayName + " - Tests";
            ServiceTestCommandHandler = new ServiceTestCommandHandlerModel();
            PopupController = CustomContainer.Get<IPopupController>();

            DuplicateTestCommand = new DelegateCommand(ServiceTestCommandHandler.DuplicateTest, () => CanDuplicateTest);
            RunAllTestsInBrowserCommand = new DelegateCommand(() => ServiceTestCommandHandler.RunAllTestsInBrowser(IsDirty));
            RunAllTestsCommand = new DelegateCommand(() => ServiceTestCommandHandler.RunAllTestsCommand(IsDirty));
            RunSelectedTestInBrowserCommand = new DelegateCommand(ServiceTestCommandHandler.RunSelectedTestInBrowser, () => CanRunSelectedTestInBrowser);
            RunSelectedTestCommand = new DelegateCommand(ServiceTestCommandHandler.RunSelectedTest, () => CanRunSelectedTest);
            StopTestCommand = new DelegateCommand(ServiceTestCommandHandler.StopTest, () => CanStopTest);
            CreateTestCommand = new DelegateCommand(CreateTests);
            DeleteTestCommand = new DelegateCommand(() => ServiceTestCommandHandler.DeleteTest(SelectedServiceTest), () => CanDeleteTest);
            CanSave = true;

            RunAllTestsUrl = WebServer.GetWorkflowUri(resourceModel, "", UrlType.Tests)?.ToString();
        }

        private bool CanDeleteTest => GetPermissions() && SelectedServiceTest != null && !SelectedServiceTest.Enabled;

        private void CreateTests()
        {
            if (IsDirty)
            {
                PopupController?.Show(Resources.Languages.Core.ServiceTestSaveEditedTestsMessage, Resources.Languages.Core.ServiceTestSaveEditedTestsHeader, MessageBoxButton.OK, MessageBoxImage.Error, null, false, true, false, false);
                return;
            }

            var testModel = ServiceTestCommandHandler.CreateTest(ResourceModel);
            AddTest(testModel);
            SelectedServiceTest = testModel;
            SelectedServiceTest.RunSelectedTestUrl = WebServer.GetWorkflowUri(ResourceModel, "", UrlType.Tests) + "/" + SelectedServiceTest.TestName;
        }

        private void AddTest(IServiceTestModel testModel)
        {
            var index = Tests.Count - 1;
            if(index >= 0)
            {
                Tests.Insert(index, testModel);
            }
            else
            {
                Tests.Add(testModel);
            }
        }

        public bool CanStopTest { get; set; }
        private bool CanRunSelectedTestInBrowser => SelectedServiceTest != null && !SelectedServiceTest.IsDirty;
        private bool CanRunSelectedTest => GetPermissions();
        public bool CanDuplicateTest => GetPermissions();

        public bool CanSave { get; set; }

        private bool GetPermissions()
        {
            return true;
        }

        public bool IsDirty
        {
            get
            {
                try
                {
                    if (Tests == null || Tests.Count <= 1)
                    {
                        return false;
                    }
                    var isDirty = Tests.Any(resource => resource.IsDirty);

                    var isConnected = ResourceModel.Environment.Connection.IsConnected;

                    return isDirty && isConnected;
                }
                // ReSharper disable once UnusedVariable
                catch (Exception ex)
                {
                    //if (!_errorShown)
                    //{
                    //    _popupController.ShowCorruptTaskResult(ex.Message);
                    //    Dev2Logger.Error(ex);
                    //    _errorShown = true;
                    //}
                }
                return false;
            }
        }

        public void Save()
        {
            try
            {
                var serviceTestModels = Tests.Where(model => model.GetType() != typeof(DummyServiceTest)).ToList();
                var duplicateTests = serviceTestModels.GroupBy(x => x.TestName).Where(group => group.Count() > 1).Select(group => group.Key);
                if (duplicateTests.Any())
                {
                    PopupController?.Show(Resources.Languages.Core.ServiceTestDuplicateTestNameMessage, Resources.Languages.Core.ServiceTestDuplicateTestNameHeader, MessageBoxButton.OK, MessageBoxImage.Error, null, false, true, false, false);
                    return;
                }
                ResourceModel.Environment.ResourceRepository.SaveTests(ResourceModel.ID, serviceTestModels);
                MarkTestsAsDirty(false);
            }
            catch (Exception)
            {
                // MarkTestsAsDirty(true);
            }

        }

        private void MarkTestsAsDirty(bool isDirty)
        {
            foreach (var model in Tests) //This is based on the fact that the save will do a bulk save all the time
            {
                model.IsDirty = isDirty;
            }
        }

        public IContextualResourceModel ResourceModel { get; }

        public IServiceTestModel SelectedServiceTest
        {
            get
            {
                return _selectedServiceTest;
            }
            set
            {
                if (value == null)
                {
                    _selectedServiceTest.PropertyChanged -= ActionsForPropChanges;
                    _selectedServiceTest = null;
                    OnPropertyChanged(() => SelectedServiceTest);
                    return;
                }
                if (Equals(_selectedServiceTest, value) || value.IsNewTest)
                {
                    return;
                }
                if (_selectedServiceTest != null)
                    _selectedServiceTest.PropertyChanged -= ActionsForPropChanges;
                _selectedServiceTest = value;
                _selectedServiceTest.PropertyChanged += ActionsForPropChanges;
                OnPropertyChanged(() => SelectedServiceTest);
            }
        }

        private void ActionsForPropChanges(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Enabled")
            {
                ViewModelUtils.RaiseCanExecuteChanged(DeleteTestCommand);
            }
        }

        public IServiceTestCommandHandler ServiceTestCommandHandler { get; set; }

        public string RunAllTestsUrl
        {
            get { return _runAllTestsUrl; }
            set
            {
                _runAllTestsUrl = value;
                OnPropertyChanged(() => RunAllTestsUrl);
            }
        }

        public string TestPassingResult
        {
            get { return _testPassingResult; }
            set
            {
                _testPassingResult = value;
                OnPropertyChanged(() => TestPassingResult);
            }
        }

        public ObservableCollection<IServiceTestModel> Tests
        {
            get
            {
                if (_tests == null)
                {
                    _tests = GetTests();
                    var dummyTest = new DummyServiceTest(CreateTests) { TestName = "Create a new test." };
                    _tests.Add(dummyTest);
                    SelectedServiceTest = dummyTest;
                }
                return _tests;

            }
            set
            {
                _tests = value;
                OnPropertyChanged(() => Tests);
            }
        }

        private ObservableCollection<IServiceTestModel> GetTests()
        {
            try
            {
                var loadResourceTests = ResourceModel.Environment.ResourceRepository.LoadResourceTests(ResourceModel.ID);
                if (loadResourceTests != null)
                {
                    var serviceTestModels = loadResourceTests.Select(to => new ServiceTestModel(ResourceModel.ID)
                    {
                        TestName = to.TestName,
                        UserName = to.UserName,
                        AuthenticationType = to.AuthenticationType,
                        Enabled = to.Enabled,
                        ErrorExpected = to.ErrorExpected,
                        NoErrorExpected = to.NoErrorExpected,
                        LastRunDate = to.LastRunDate,
                        TestPending = to.TestPending,
                        TestFailing = to.TestFailing,
                        TestPassed = to.TestPassed,
                        Password = to.Password,
                        TestInvalid = to.TestInvalid,
                        Inputs = to.Inputs?.Select(input => new ServiceTestInput(input.Variable, input.Value) as IServiceTestInput).ToList(),
                        Outputs = to.Outputs?.Select(output => new ServiceTestOutput(output.Variable, output.Value) as IServiceTestOutput).ToList()
                    });
                    return serviceTestModels.ToObservableCollection<IServiceTestModel>();
                }
            }
            catch (Exception)
            {
                return new ObservableCollection<IServiceTestModel>();
            }
            return new ObservableCollection<IServiceTestModel>();
        }

        public ICommand DeleteTestCommand { get; set; }
        public ICommand DuplicateTestCommand { get; set; }
        public ICommand RunAllTestsInBrowserCommand { get; set; }
        public ICommand RunAllTestsCommand { get; set; }
        public ICommand RunSelectedTestInBrowserCommand { get; set; }
        public ICommand RunSelectedTestCommand { get; set; }
        public ICommand StopTestCommand { get; set; }
        public ICommand CreateTestCommand { get; set; }

        public string DisplayName
        {
            get
            {
                if (IsDirty)
                {
                    if (!_displayName.EndsWith(" *"))
                    {
                        _displayName += " *";
                    }
                    return _displayName;
                }
                var displayName = _displayName.Replace("*", "").TrimEnd(' ');
                return displayName;
            }
            set
            {
                _displayName = value;
                OnPropertyChanged(() => DisplayName);
            }
        }

        public void Dispose()
        {
        }

        public void UpdateHelpDescriptor(string helpText)
        {
            var mainViewModel = CustomContainer.Get<IMainViewModel>();
            mainViewModel?.HelpViewModel.UpdateHelpText(helpText);
        }
    }
}