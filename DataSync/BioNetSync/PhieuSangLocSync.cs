using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNetModel.Data;
using BioNetModel;
using Bionet.API.Models;
using System.Net;
using System.Security;
using System.Web.Script.Serialization;
using System.Web;
using System.Data.Linq;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Security.Cryptography;
using DataSync;
using System.Data;
using System.Reflection;
using DataSync.BioNetSync;
using System.Drawing;


namespace DataSync.BioNetSync
{
    public class PhieuSangLocSync
    {
        private static BioNetDBContextDataContext db = null;
        private static string linkGetPhieuSangLoc = "/api/trungtamsangloc/getall?keyword=&page=0&pagesize=20";
        private static string linkPostPhieuSangLoc = "/api/trungtamsangloc/updateFromApp";

        public static PsReponse GetThongTinTrungTam()
        {
            PsReponse res = new PsReponse();
            res.Result = true;
            try
            {
                ProcessDataSync cn = new ProcessDataSync();
                db = cn.db;
                var account = db.PSAccount_Syncs.FirstOrDefault();
                if (account != null)
                {
                    string token = cn.GetToken(account.userName, account.passWord);
                    if (!string.IsNullOrEmpty(token))
                    {
                        var result = cn.GetRespone(cn.CreateLink(linkGetPhieuSangLoc), token);
                        if (result.Result)
                        {
                            string json = result.ValueResult;
                            JavaScriptSerializer jss = new JavaScriptSerializer();
                            ObjectModel.RootObjectAPI Repo = jss.Deserialize<ObjectModel.RootObjectAPI>(json);
                            if (Repo != null)
                            {
                                if (Repo.TotalCount > 0)
                                {
                                    var item = Repo.Items[0];
                                    PSThongTinTrungTam tt = new PSThongTinTrungTam();
                                    tt.Diachi = item["DiaChiTTSL"];
                                    tt.DienThoai = item["SDTTTSL"];
                                    tt.ID = item["ID"];
                                    tt.LicenseKey = item["SDTTTSL"];
                                    var Logo = item["LicenseKey"];
                                    if (Logo != null)
                                    {
                                        try
                                        {
                                            byte[] b = Logo.ToArray();
                                            //MemoryStream ms = new MemoryStream(b);
                                            //Image img = Image.FromStream(ms);
                                            tt.Logo = b;
                                        }
                                        catch { }
                                    }
                                    tt.MaTrungTam = item["MaTTSL"];
                                    //   tt.MaVietTat =  item["MaTTSL"].t;
                                    tt.TenTrungTam = item["TenTTSL"];
                                    var resup = UpdateThongTinTrungTam(tt);
                                    if (!resup.Result)
                                    {
                                        res.Result = false;
                                        res.StringError = resup.StringError;
                                    }
                                }
                            }
                            else
                            {
                                res.Result = false;
                                res.StringError = result.ErorrResult;
                            }
                        }
                        else
                        {
                            res.Result = false;
                            res.StringError = result.ErorrResult;
                        }
                    }
                    else
                    {
                        res.Result = false;
                        res.StringError = "Kiểm tra lại kết nối mạng hoặc tài khoản đồng bộ!";
                    }

                }
                else
                {
                    res.Result = false;
                    res.StringError = "Chưa có  tài khoản đồng bộ!";
                }

            }
            catch (Exception ex)
            {
                res.Result = false;
                res.StringError = DateTime.Now.ToString() + "Lỗi khi get dữ liệu Danh Mục Mapping Kỹ Thuật - Dịch Vụ từ server \r\n " + ex.Message;

            }
            return res;
        }
        public static PsReponse UpdateThongTinTrungTam(PSThongTinTrungTam tt)
        {

            PsReponse res = new PsReponse();

            try
            {
                ProcessDataSync cn = new ProcessDataSync();
                db = cn.db;
                db.Connection.Open();
                db.Transaction = db.Connection.BeginTransaction();
                PSThongTinTrungTam ttam = db.PSThongTinTrungTams.FirstOrDefault();
                if (ttam == null)
                {
                    PSThongTinTrungTam ttnew = new PSThongTinTrungTam();
                    ttnew.Diachi = Encoding.UTF8.GetString(Encoding.Default.GetBytes(tt.Diachi));
                    ttnew.DienThoai = tt.DienThoai;
                    ttnew.ID = tt.ID;
                    ttnew.LicenseKey = tt.LicenseKey;
                    ttnew.Logo = tt.Logo;
                    ttnew.MaTrungTam = tt.MaTrungTam;
                    ttnew.MaVietTat = tt.MaTrungTam.Substring(1, 2);
                    ttnew.TenTrungTam = Encoding.UTF8.GetString(Encoding.Default.GetBytes(tt.TenTrungTam));
                    db.PSThongTinTrungTams.InsertOnSubmit(ttnew);
                    db.SubmitChanges();
                    res.Result = true;
                }
                else
                 if (string.IsNullOrEmpty(ttam.ID))
                {
                    res.Result = true;
                    db.PSThongTinTrungTams.FirstOrDefault().LicenseKey = tt.LicenseKey;
                    db.PSThongTinTrungTams.FirstOrDefault().ID = tt.ID;
                    db.SubmitChanges();
                }
                else
                if (ttam.ID == tt.ID && ttam.MaTrungTam == tt.MaTrungTam)
                {
                    if (ttam.isDongBo != false)
                    {
                        res.Result = true;
                        db.PSThongTinTrungTams.FirstOrDefault().LicenseKey = tt.LicenseKey;
                        db.PSThongTinTrungTams.FirstOrDefault().isDongBo = true;
                        db.SubmitChanges();
                    }
                }
                else
                {
                    res.Result = false;
                    res.StringError = "ID giữa Trung tâm và tổng cục không tương xứng, vui lòng kiểm tra lại!";
                }
                db.Transaction.Commit();
                db.Connection.Close();


            }
            catch (Exception ex)
            {
                db.Transaction.Rollback();
                db.Connection.Close();
                res.Result = false;
                res.StringError = ex.ToString();
            }
            return res;
        }
    }
}