using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;


namespace vas.Dynamics.Crm.CustomWorkflowPlugin
{
    public class QuoteProducts: CodeActivity
    {
        protected override void Execute(CodeActivityContext executionContext)
        {
            try
            {
                IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
                IOrganizationServiceFactory serviceFactory =
                    executionContext.GetExtension<IOrganizationServiceFactory>();
                IOrganizationService service =
                    serviceFactory.CreateOrganizationService(context.UserId);

                EntityReference quoteRef = this.inputQuote.Get(executionContext);
                EntityReference productRef = this.inputProduct.Get(executionContext);

                if (quoteRef == null)
                {
                    throw new InvalidOperationException("The input quote has not been specified", new ArgumentNullException("InputQuote"));
                }

                if (productRef == null)
                {
                    throw new InvalidOperationException("The input product has not been specified", new ArgumentNullException("InputProduct"));
                }

                Entity productEntity;
                {
                    //Create a request
                    RetrieveRequest retrieveRequest = new RetrieveRequest();
                    retrieveRequest.ColumnSet = new ColumnSet(new string[] { "productid", "productnumber", "defaultuomid" });
                    retrieveRequest.Target = productRef;

                    //Execute the request
                    RetrieveResponse retrieveResponse = (RetrieveResponse)service.Execute(retrieveRequest);

                    //Retrieve the Product Entity
                    productEntity = retrieveResponse.Entity as Entity;
                }

                // Retrieve the id
                Guid quoteid = quoteRef.Id;
                Guid productid = productRef.Id;

                //Retreive the defaultUomId
                string uomid = string.Empty;
                if (productEntity != null)
                {
                    if (productEntity.Attributes.Contains("defaultuomid"))
                        uomid = productEntity.Attributes["defaultuomid"].ToString();
                }

                // Create a quote product and add to quote
                Entity quoteProduct = new Entity();
                quoteProduct.LogicalName = "quotedetail";
                quoteProduct["productid"] = new EntityReference("product", productid);
                quoteProduct["uomid"] = new EntityReference("uom", new Guid(uomid));
                quoteProduct["quoteid"] = new EntityReference("quote", quoteid);
                quoteProduct["quantity"] = 1M;
                Guid quotedetailId = service.Create(quoteProduct);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        }

        // Define Input/Output Arguments
        [RequiredArgument]
        [Input("InputQuote")]
        [ReferenceTarget("quote")]
        public InArgument<EntityReference> inputQuote { get; set; }

        [RequiredArgument]
        [Input("InputProduct")]
        [ReferenceTarget("product")]
        public InArgument<EntityReference> inputProduct { get; set; }

       
    }
}
