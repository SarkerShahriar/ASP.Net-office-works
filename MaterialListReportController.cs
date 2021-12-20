using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using Supply_Chain_Management.Helper;
using Supply_Chain_Management.Models;
using Supply_Chain_Management.ReportDataset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Supply_Chain_Management.Controllers
{
    public class MaterialListReportController : Controller
    {
        private SCMSContext db = new SCMSContext();
        // GET: MaterialListReport
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult MaterialList()
            {
            var CompanyUnitId = Convert.ToInt32(Session["companyid"].ToString());
            var prodCode = (from supplierGrade in db.Suppliers_Grade
                            join inventory in db.Inv_RM_Inventory on supplierGrade.Id equals inventory.Suppliers_GradeId
                            select new { Id = supplierGrade.Id, Code = supplierGrade.Code }
                         ).Distinct().ToList();

            //ViewBag.ProductCode = new SelectList(prodCode, "Id", "Code").Distinct();
            ViewBag.ProductCode = new SelectList(prodCode, "Code", "Code").Distinct();
            return View();
            }

        [HttpPost]
        public ActionResult MaterialList(string ProductCode, DateTime? dateFrom, DateTime? dateTo)
            {
            var CompanyUnitId = Convert.ToInt32(Session["companyid"].ToString());

            MaterialListDS DSML = new MaterialListDS();

            DateTime newdateto = dateTo ?? DateTime.Now;

            newdateto = newdateto.AddDays(1);

            var MatList = (from supgrade in db.Suppliers_Grade
                           join invReqDet in db.Inv_RequisitionDet on supgrade.Id equals invReqDet.Suppliers_GradeId
                           join umo in db.UOM on supgrade.UOMId equals umo.Id
                           join invreMAS in db.Inv_RequisitionMas on invReqDet.Inv_RequisitionMasId equals invreMAS.Id
                           where invreMAS.CompanyUnitId == CompanyUnitId
                           && (dateFrom == null || (invReqDet.RequireDate >= dateFrom && invReqDet.RequireDate <= newdateto))
                           && (ProductCode == null || (supgrade.Code == ProductCode))
                           select new
                               {
                               ReqDate = invReqDet.RequireDate,
                               MaterialCode = supgrade.Code,
                               MaterialName = supgrade.MaterialName,
                               UOM = umo.UOMName,
                               ReqQTY = invReqDet.ReqQty
                               }
                           ).Distinct().ToList();

            foreach (var mList in MatList)
                {
                DSML.MaterialList.AddMaterialListRow(
                    NullHelper.DateToString(mList.ReqDate),
                    mList.MaterialCode,
                    mList.MaterialName,
                    mList.UOM,
                    mList.ReqQTY
                    );
                }

            if (ProductCode != null)
                {
                MatList = MatList.Where(x => x.MaterialCode == ProductCode).ToList();
                }

            var MinDate = NullHelper.DateToString( DateTime.Now);
            var MaxDate = NullHelper.DateToString(DateTime.Now);

            var company = db.CompanyUnit.SingleOrDefault(x => x.Id == CompanyUnitId);
            if (dateFrom == null)
                {
                if (MatList.Count() > 0)
                    {
                    MinDate = NullHelper.DateToString( MatList.OrderBy(x => x.ReqDate).FirstOrDefault().ReqDate);
                    MaxDate = NullHelper.DateToString(MatList.OrderByDescending(x => x.ReqDate).FirstOrDefault().ReqDate);
                    }

                }
            else
                {
                MinDate = NullHelper.DateToString(dateFrom) ?? NullHelper.DateToString(DateTime.Now);
                MaxDate = NullHelper.DateToString(dateTo) ?? NullHelper.DateToString(DateTime.Now);
                }


            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "MaterialLIST.rpt"));

            rd.SetDataSource(DSML);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            rd.Close();
            rd.Dispose();
            return null;
            }
        }
}