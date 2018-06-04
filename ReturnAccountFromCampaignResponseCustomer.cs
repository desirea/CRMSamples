// =====================================================================
//  This code adds a contract item to a case. 
//
//  Copyright (C) Valley Agriculture Software.  All rights reserved.
//
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
using Microsoft.Xrm.Sdk.Metadata;

// These namespaces are found in the Microsoft.Xrm.Sdk.Workflow.dll assembly
// located in the SDK\bin folder of the SDK download.
using Microsoft.Xrm.Sdk.Workflow;

// These namespaces are found in the Microsoft.Crm.Sdk.Proxy.dll assembly
// located in the SDK\bin folder of the SDK download.
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Crm.Sdk;


namespace vas.Dynamics.Crm.CustomWorkflowPlugin
{
    public sealed partial class ReturnAccountFromCampaignResponseCustomer: CodeActivity
    {


        // Define Input/Output Arguments to retrieve the case and the account
        [RequiredArgument]
        [Input("Input Campaign Response")]
        [ReferenceTarget("campaignresponse")]
        public InArgument<EntityReference> inputContract { get; set; }
     

        [Output("Account")]
        [ReferenceTarget("account")]
        public OutArgument<EntityReference> outAccount { get; set; }


        private Guid _campaignResponseId;

        protected override void Execute(CodeActivityContext executionContext)
        {
            try
            {
                // Create tracing service for the plugin trace
                ITracingService tracingService = executionContext.GetExtension<ITracingService>();

                tracingService.Trace("Create Service");

                #region Retrieve the execution context service.
                IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
                IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                #endregion


                tracingService.Trace("Retrieve Campaign Response");
                #region Retrieve the campaign response.
                tracingService.Trace("Retrieve the GUID for campaign response");
                // Retrieve the GUID for campaign response
                _campaignResponseId = this.inputContract.Get(executionContext).Id;



                #endregion


                // Instantiate QueryExpression QEaccount
                var QEaccount = new QueryExpression("account");

                // Add columns to QEaccount.ColumnSet
                QEaccount.ColumnSet.AddColumns("accountnumber", "accountid");

                // Add link-entity QEaccount_activityparty
                var QEaccount_activityparty = QEaccount.AddLink("activityparty", "accountid", "partyid");
                QEaccount_activityparty.EntityAlias = "partyid";

                // Add link-entity QEaccount_activityparty_campaignresponse
                var QEaccount_activityparty_campaignresponse = QEaccount_activityparty.AddLink("campaignresponse", "activityid", "activityid");
                QEaccount_activityparty_campaignresponse.EntityAlias = "cr";

                // Define filter QEaccount_activityparty_campaignresponse.LinkCriteria
                QEaccount_activityparty_campaignresponse.LinkCriteria.AddCondition("activityid", ConditionOperator.Equal, _campaignResponseId);


                tracingService.Trace("Create EntityCollection to store entity results and retrieve Account.");
                //Create EntityCollection to store entity results and retrieve contracts. Just in case there are multiple contracts associated. 
                EntityCollection crAccounts = service.RetrieveMultiple(QEaccount);
                Entity _cracct = new Entity();

                tracingService.Trace("Return Total Accounts " + crAccounts.TotalRecordCount);
                tracingService.Trace("Return  Accounts: " + crAccounts.EntityName);
                tracingService.Trace("Return  Accounts: " + crAccounts.Entities.Count);
                tracingService.Trace("Return Accounts: " + crAccounts.Entities.ToString());

                if (crAccounts.Entities.Count > 0)
                {
                    //Iterate the collection for the contractline which should only be one.
                    foreach (var cracct in crAccounts.Entities)
                    {

                        tracingService.Trace("Return  Campaign Responses: " + cracct.Attributes["accountid"]);
                        // Assign Contract to Contract Entity property
                        _cracct = cracct;
                    }
                    tracingService.Trace("Return Found Account");
                    outAccount.Set(executionContext, new EntityReference("account", (Guid)_cracct.Attributes["accountid"]));


                }


            }catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }
    }
}
