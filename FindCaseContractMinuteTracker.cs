// =====================================================================
//  This code updates custom fields on a case
// =====================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;

// These namespaces are found in the Microsoft.Xrm.Sdk.dll assembly
// located in the SDK\bin folder of the SDK download.
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

// These namespaces are found in the Microsoft.Xrm.Sdk.Workflow.dll assembly
// located in the SDK\bin folder of the SDK download.
using Microsoft.Xrm.Sdk.Workflow;

// These namespaces are found in the Microsoft.Crm.Sdk.Proxy.dll assembly
// located in the SDK\bin folder of the SDK download.
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Crm.Sdk;

//Add Helper Files

namespace vas.Dynamics.Crm.CustomWorkflowPlugin.ContractCaseBilling
{
    public sealed partial class FindCaseContractMinuteTracker : CodeActivity
    {

        // Define Input/Output Arguments to retrieve the case and the account
        [RequiredArgument]
        [Input("Input_Contract")]
        [ReferenceTarget("contract")]
        public InArgument<EntityReference> inputContract { get; set; }

        [RequiredArgument]
        [Input("Input_Account")]
        [ReferenceTarget("account")]
        public InArgument<EntityReference> inputAccount { get; set; }

        [Output("MinuteTracker")]
        [ReferenceTarget("new_casecontractminutetracker")]
        public OutArgument<EntityReference> outMinuteTracker { get; set; }
        private Guid _accountID;
        private Guid _contractId;
        /// <summary>
        /// This method first retrieves the contract and account from the interface then finds and returns the related CCMT
        /// </summary>
        protected override void Execute(CodeActivityContext executionContext)
        {
            try
            {
                // Create tracing service for the plugin trace
                ITracingService tracingService = new TimestampedTracingService(executionContext);//executionContext.GetExtension<ITracingService>();

                tracingService.Trace("Create Service");

                #region Retrieve the execution context service.
                IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
                IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                #endregion

                tracingService.Trace("Retrieve Contract");
                #region Retrieve the contract.
                tracingService.Trace("Retrieve the GUID for contract");
                // Retrieve the GUID for contract
                _contractId = this.inputContract.Get(executionContext).Id;

                // Guid uomId = this.inputUomid.Get(executionContext).Id;
                tracingService.Trace("Request Entity for Contract");
                // Request an entity for Contract
                RetrieveRequest requestContract = new RetrieveRequest();
                requestContract.ColumnSet = new ColumnSet(true);
                requestContract.Target = new EntityReference("contract", _contractId);

                tracingService.Trace("Create Contract Entity");
                // Execute requests to retrieve Contract entity
                Entity contract = (Entity)((RetrieveResponse)service.Execute(requestContract)).Entity;

                #endregion
                tracingService.Trace("Retrieve Account");
                #region Retrieve the account to search for a account.
                tracingService.Trace("Retrieve the GUID for Account");
                // Retrieve the GUID for account
                _accountID = this.inputAccount.Get(executionContext).Id;

                // Guid uomId = this.inputUomid.Get(executionContext).Id;
                tracingService.Trace("Request Entity for Account");
                // Request an entity for Order
                RetrieveRequest requestAccount = new RetrieveRequest();
                requestAccount.ColumnSet = new ColumnSet(true);
                EntityReference accountER = new EntityReference("account", _accountID);
                requestAccount.Target = accountER;

                tracingService.Trace("Create Account Entity");
                // Execute requests to retrieve Contract entity
                Entity account = (Entity)((RetrieveResponse)service.Execute(requestAccount)).Entity;

                #endregion

                tracingService.Trace("Query Case Contract Minute Tracker");
                // Instantiate QueryExpression QEnew_casecontractminutetracker
                // CREATE QUERYEXPRESSION
                // Instantiate QueryExpression QEnew_casecontractminutetracker
                var ccmtQueryExpression = new QueryExpression("new_casecontractminutetracker");

                // Add columns to QEnew_casecontractminutetracker.ColumnSet
                ccmtQueryExpression.ColumnSet.AddColumns("new_totalprepaidminutesused", "new_name", "new_availableprepaidminutes", "new_contractenddate", "new_account", "new_totalunbilledminutesused", "new_contract", "new_contractstartdate", "new_totalbilledminutesused");

                // Add link-entity QEnew_casecontractminutetracker_account
                var QEnew_casecontractminutetracker_account = ccmtQueryExpression.AddLink("account", "new_account", "accountid");
                QEnew_casecontractminutetracker_account.EntityAlias = "acct";

                // Define filter QEnew_casecontractminutetracker_account.LinkCriteria
                QEnew_casecontractminutetracker_account.LinkCriteria.AddCondition("accountid", ConditionOperator.Equal, _accountID);

                // Add link-entity QEnew_casecontractminutetracker_contract
                var QEnew_casecontractminutetracker_contract = ccmtQueryExpression.AddLink("contract", "new_contract", "contractid");
                QEnew_casecontractminutetracker_contract.EntityAlias = "cntrct";

                // Define filter QEnew_casecontractminutetracker_contract.LinkCriteria
                QEnew_casecontractminutetracker_contract.LinkCriteria.AddCondition("contractid", ConditionOperator.Equal, _contractId);



                tracingService.Trace("Create EntityCollection to store entity results and retrieve CCMT.");
                //Create EntityCollection to store entity results and retrieve contracts. Just in case there are multiple contracts associated. 
                EntityCollection caseContractMinuteTrackers = service.RetrieveMultiple(ccmtQueryExpression);
                Entity _ccmt = new Entity();
                tracingService.Trace("Return Total Case Contract Minute Trackers: " + caseContractMinuteTrackers.TotalRecordCount);
                tracingService.Trace("Return  Case Contract Minute Trackers: " + caseContractMinuteTrackers.EntityName);
                tracingService.Trace("Return  Case Contract Minute Trackers: " + caseContractMinuteTrackers.Entities.Count);
                tracingService.Trace("Return  Case Contract Minute Trackers: " + caseContractMinuteTrackers.Entities.ToString());
                if (caseContractMinuteTrackers.Entities.Count > 0)
                {
                    //Iterate the collection for the contractline which should only be one.
                    foreach (var ccmt in caseContractMinuteTrackers.Entities)
                    {
                        // Assign Contract to Contract Entity property
                        _ccmt = ccmt;
                    }
                    tracingService.Trace("Return Found Case Contract Minute Tracker");
                    outMinuteTracker.Set(executionContext, new EntityReference("new_casecontractminutetracker", (Guid)_ccmt.Attributes["new_casecontractminutetrackerid"]));


                }
            } catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
