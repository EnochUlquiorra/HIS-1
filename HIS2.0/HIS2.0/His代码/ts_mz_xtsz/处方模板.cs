using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using TrasenFrame.Classes;
using TrasenClasses.GeneralControls;
using TrasenClasses.GeneralClasses;
using ts_mz_class;
using TrasenFrame.Forms;
using YpClass;
using ts_mzys_class;


namespace ts_mz_xtsz
{
    public partial class Frmblcf : Form
    {
        private Form _mdiParent;
        private MenuTag _menuTag;
        private string _chineseName;
        private DataSet PubDset = new DataSet();
        public struct Cf
        {
            public long mbid;
            public int js;
            public int ksdm;
            public int ysdm;
            public int zxksid;
            public int zyksid;
            /// <summary>
            /// 项目来源
            /// </summary>
            public int xmly;
            /// <summary>
            /// 套餐ID
            /// </summary>
            public long tcid;
            public string fpcode;
            public string tjdxmdm;
            public string cfh;

        }
        public  struct Cell
        {
            public int nrow ;
            public int ncol ;
        }
        public Cf Dqcf = new Cf();
        public Cell cell = new Cell();
        public DataTable Tab; //所有未收费的处方明细
        private DataTable Tbks;//挂号科室数据
        private DataTable Tbys;//挂号医生数据
        
        private FrmCard_YZ f;//选项卡
        private string sNum = "";//当前单元格的数量

        private int Xmly = 0;//项目来源　用于控制项目选择范围 0 全部 1 药品  2 项目

        private bool BDelRow = false; //是否正在删除行

        private long Jgbm = 0;//机构编码

        private DataSet _dataSet = new DataSet();

        private SystemCfg psItem;

        public Frmblcf(MenuTag menuTag, string chineseName, Form mdiParent)
        {
            InitializeComponent();


            string ssql = "select cast(id as int) id,dwmc name,rtrim(pym) pym from yp_ypdw ";
            DataTable tb1 = InstanceForm.BDatabase.GetDataTable(ssql);
            tb1.TableName = "ypdw";
            _dataSet.Tables.Add(tb1);


            ssql = "select cast(id as int) id,name, rtrim(py_code) pym from JC_FREQUENCY  ";
            DataTable tb2 = InstanceForm.BDatabase.GetDataTable(ssql);
            tb2.TableName = "pc";
            _dataSet.Tables.Add(tb2);

            ssql = "select cast(id as int) id,name,rtrim(py_code) pym from jc_usagediction ";
            DataTable tb3 = InstanceForm.BDatabase.GetDataTable(ssql);
            tb3.TableName = "yf";
            _dataSet.Tables.Add(tb3);
            

            _menuTag = menuTag;
            _chineseName = chineseName;
            _mdiParent = mdiParent;
            
            this.Text = _chineseName;
        }

        private void Frmhjsf_Load(object sender, EventArgs e)
        {
            Jgbm = TrasenFrame.Forms.FrmMdiMain.Jgbm;
            //初始化网格，邦定一个空结果集
            //Tab = mzys.Select_cf(0, 0, 0, 0, 0,0);
            //AddPresc(Tab);
            //挂号科室
            Tbks = Fun.GetGhks(false);
            //挂号医生
            Tbys = Fun.GetGhys(0);
            //医保类型
            //???FunAddComboBox.AddYblx(true, 0, cmbyblx);
            this.WindowState = FormWindowState.Maximized;

            //ini文件读取
            string Yxfss = ApiFunction.GetIniString("划价收费", "划价优先非实时查询", Constant.ApplicationDirectory + "//ClientWindow.ini");
            string yflx = ApiFunction.GetIniString("划价收费", "划价优先住院药房", Constant.ApplicationDirectory + "//ClientWindow.ini");
            if (yflx.Trim() == "true")
                this.rdozyyf.Checked = true;
            else
                this.rdomzyf.Checked = true;
            string xmly = "0";

            if (xmly == "0") Xmly = 0;
            if (xmly == "1") Xmly = 1;
            if (xmly == "2") Xmly = 2;

            f = new FrmCard_YZ(_menuTag, "", _mdiParent);
            if (Yxfss.Trim() == "true")
                f.checkBox1.Checked = true;

            //刷新内存收费项目
            butsxxm_Click(sender, e);

            Select_Cyxm();
            Select_Cyyp();
            Select_Mb();
            DataTable tbmbmx= Read_Mb(0);
            AddPresc(tbmbmx);
            this.tabControl4.SelectedTab = this.tabPage6;

        }

        //窗体键盘事件
        private void Frmhjsf_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2 && butsave.Enabled == true)
            {
                butsave_Click(sender, e);
            }
            if (e.KeyCode == Keys.F3 && butnew.Enabled == true)
            {
                butnew_Click(sender, e);
            }
            if (e.KeyCode == Keys.F5 && butsxxm.Enabled == true)
            {
                butsxxm_Click(sender, e);
            }
            if (e.KeyCode == Keys.F8 )
            {
                //butsf_Click(sender, e);
            }

        }

        //添加网格行
        private void Addrow(DataRow ReturnRow,ref int nrow )
        {
            DataTable tb = (DataTable)dataGridView1.DataSource;
            DataRow row = tb.Rows[nrow];
            DataRow[] rows1 = tb.Select("hjid='" + Convert.ToInt64(Convertor.IsNull(row["hjid"],"0")) + "' and 项目ID>0");
            int xmly = Convert.ToInt32(ReturnRow["项目来源"]);
            long xmid =0;
            long cjid =0;
            if (xmly == 1)
            {
                xmid = Convert.ToInt64(ReturnRow["ggid"]);
                cjid = Convert.ToInt64(ReturnRow["项目ID"]);
            }
            else
                xmid = Convert.ToInt64(ReturnRow["yzid"]);
           
            long zxks = Convert.ToInt32(Convertor.IsNull(ReturnRow["zxksid"],"0"));

            //变量定义
            string jl = "1";
            string jldw = "";
            string jldwid="";
            string dwlx="0";
            string pcmc = "";
            string pcid = "0";
            string yfmc = "";
            string yfid = "";

            //中药默认用法和频次
            if (Dqcf.tjdxmdm == "03" && rows1.Length > 0)
            {
                pcmc = rows1[0]["频次"].ToString();
                pcid = rows1[0]["频次id"].ToString();
                yfmc = rows1[0]["用法"].ToString();
                yfid = rows1[0]["用法id"].ToString();
            }

            string ssql = "";
            if (xmly == 1)
                ssql = "select cast(hlxs as float) hlxs,hldw,syff,dbo.fun_getUsageName(syff) yfmc,lsj,pfj,shh,yppm,ypspm,ypgg,s_ypdw,s_sccj,psyp from vi_yf_kcmx where cjid=" + cjid + " and deptid=" + zxks + "";
            else
            {
                ssql = "select order_unit as item_unit,0 cost_price ,'' py_code,'' std_code,order_name item_name from jc_hoitemdiction  where order_id=" + xmid + "";
            }
            DataTable tb_xmyp = InstanceForm.BDatabase.GetDataTable(ssql);


            row["医嘱内容"] = ReturnRow["项目内容"];

            if (xmly == 1)
            {
                jl = tb_xmyp.Rows[0]["hlxs"].ToString();
                jldw = Yp.SeekYpdw(Convert.ToInt32(tb_xmyp.Rows[0]["hldw"]),InstanceForm.BDatabase);
                jldwid = tb_xmyp.Rows[0]["hldw"].ToString();
                dwlx = "1";
                if (yfmc == "")
                {
                    yfmc = tb_xmyp.Rows[0]["yfmc"].ToString();
                    yfid = tb_xmyp.Rows[0]["syff"].ToString();
                }
                if (Dqcf.tjdxmdm == "03")
                    jl = "10";
            }
            else
            {
                jl = "1";
                jldw = tb_xmyp.Rows[0]["item_unit"].ToString();
                jldwid = "0";
                dwlx = "0";
                yfmc = "";
                yfid = "0";
                row["单价"] = tb_xmyp.Rows[0]["cost_price"].ToString();
                row["单位"] = tb_xmyp.Rows[0]["item_unit"].ToString();
                row["金额"] = tb_xmyp.Rows[0]["cost_price"].ToString();
            }



            row["剂量"] = jl;
            row["剂量单位"] = jldw;
            row["频次"] = pcmc;
            row["用法"] = yfmc;
            row["天数"] = "1";
            row["嘱托"] = "";
            row["剂数"] = Dqcf.js.ToString();
              
            //计算

            row["剂量单位ID"] = jldwid;
            row["dwlx"] = dwlx;

            row["序号"] = " 1";
            //如果已有划价ID则不替换
            if (Convert.ToInt32(Convertor.IsNull(row["HJID"], "0")) == 0)
                row["HJID"] = "0";

            row["频次ID"] = pcid;
            row["用法ID"] = yfid;
            row["统计大项目"] = Convertor.IsNull(ReturnRow["statitem_code"], "");
            row["项目ID"] = xmid;// ReturnRow["项目id"];
            //如果已有划价明细ID则不替换
            if (Convert.ToInt32(Convertor.IsNull(row["HJMXID"], "0")) == 0)
                row["HJMXID"] = "0";

            row["自备药"] = "0";

            row["处方分组序号"] = "0";
            row["排序序号"] = "0";
            if (zxks==0)
            {
                row["执行科室"] = "";
                row["执行科室id"] = "0";
            }
            else
            {
                row["执行科室"] = Convertor.IsNull(ReturnRow["执行科室"], "");
                row["执行科室id"] = Convertor.IsNull(ReturnRow["zxksid"], "0");
            }

            row["项目来源"] = ReturnRow["项目来源"];
            row["选择"] = true;
            row["修改"] = true;
            row["cjid"] = cjid.ToString();
            Seek_Price(row);
            Dqcf.zxksid = Convert.ToInt32(Convertor.IsNull(ReturnRow["zxksid"], "0"));
            Dqcf.xmly = Convert.ToInt32(Convertor.IsNull(ReturnRow["项目来源"], "0"));
            Dqcf.tjdxmdm = Convert.ToString(Convertor.IsNull(ReturnRow["statitem_code"], ""));
            tb.AcceptChanges();
            dataGridView1.DataSource = tb;


        }
        //计算用量和价格
        private void Seek_Price(DataRow row)
        {
            int xmly =Convert.ToInt32( Convertor.IsNull(row["项目来源"], "0"));
            if (xmly == 1)
            {
                int dwlx = Convert.ToInt32(row["dwlx"]);
                decimal jl = Convert.ToDecimal(Convertor.IsNull(row["剂量"], "0"));
                int pcid = Convert.ToInt32(Convertor.IsNull(row["频次id"], "0"));
                pc pc = new pc(pcid);
                decimal ts = Convert.ToDecimal(Convertor.IsNull(row["天数"], "0"));
                int js=Convert.ToInt32(Convertor.IsNull(row["剂数"], "0"));
                int cjid = Convert.ToInt32(row["cjid"]);
                int yfid = Convert.ToInt32(row["执行科室id"]);
                DataTable tb = null;
                if (Dqcf.tjdxmdm!="03")
                    tb=mzys.Seek_Yp_Price(dwlx, jl, pc.zxcs, pc.jgts, ts, cjid, yfid, 0);
                else
                    tb = mzys.Seek_Yp_Price(dwlx, jl, 1, 1, 1, cjid, yfid, 0);

                row["单价"] = tb.Rows[0]["price"];
                //row["单价可改"] = false;
                row["修改"] = true;
                row["数量"] = tb.Rows[0]["yl"];
                row["单位"] = tb.Rows[0]["unit"];
                if (Dqcf.tjdxmdm != "03")
                    row["金额"] = tb.Rows[0]["sdvalue"];
                else
                    row["金额"] = Convert.ToDecimal(tb.Rows[0]["sdvalue"]) * js;
                //row["YDWBL"] = tb.Rows[0]["ydwbl"];
                //row["批发价"] = tb.Rows[0]["pfj"];
                //row["批发金额"] = tb.Rows[0]["pfje"];
            }
            else
            {
                decimal jl = Convert.ToDecimal(Convertor.IsNull(row["剂量"],"0"));
                decimal price = Convert.ToDecimal(Convertor.IsNull(row["单价"],"0"));
                int pcid = Convert.ToInt32(Convertor.IsNull(row["频次id"],"0"));
                pc pc = new pc(pcid);
                decimal ts = Convert.ToDecimal(Convertor.IsNull(row["天数"],"0"));

                decimal _sl = jl * pc.zxcs * ts / pc.jgts;
                decimal sl = Math.Ceiling(_sl);
                decimal je = sl * price;

                row["单价"] = price.ToString();
                //if (price == 0)
                //    row["单价可改"] = true;
                row["修改"] = true;
                row["数量"] = sl.ToString();
                //row["单位"] = tb.Rows[0]["unit"];
                row["金额"] = je.ToString();
                //row["YDWBL"] = "1";
                //row["批发价"] = "0";
                //row["批发金额"] = "0";
            }
        }


        //添加未收费的处方
        private void AddPresc(DataTable tb)
        {
            
            decimal sumje = 0;
            DataTable tbmx = tb.Clone();

            string[] GroupbyField ={ "HJID" };
            string[] ComputeField ={ "金额" };
            string[] CField ={ "sum" };
            TrasenFrame.Classes.TsSet xcset = new TrasenFrame.Classes.TsSet();
            xcset.TsDataTable = tb;
            DataTable tbcf = xcset.GroupTable(GroupbyField, ComputeField, CField, "序号<>'小计'");
            bool b_ks = false;
            for (int i = 0; i <= tbcf.Rows.Count - 1; i++)
            {

                DataRow[] rows = tb.Select("HJID=" + tbcf.Rows[i]["hjid"].ToString().Trim() + "");
                for (int j = 0; j <= rows.Length - 1; j++)
                {
                    DataRow row = tb.NewRow();
                    row = rows[j];
                    row["序号"] =" "+ Convert.ToString(j + 1);
                    if (row["自备药"].ToString() == "1") row["医嘱内容"] = row["医嘱内容"] + " 【自备】";
                    if (row["处方分组序号"].ToString() == "1") { b_ks = true; row["医嘱内容"] = "┌" + row["医嘱内容"].ToString(); }
                    if (row["处方分组序号"].ToString() == "0" && b_ks == true) {row["医嘱内容"] = "│" + row["医嘱内容"].ToString(); }
                    if (row["处方分组序号"].ToString() == "-1" && b_ks == true) { b_ks = false; row["医嘱内容"] = "└" + row["医嘱内容"].ToString(); }
                    
                    tbmx.ImportRow(row);

                    

                }
                //DataRow sumrow = tbmx.NewRow();
                //sumrow["序号"] = " 小计";
                //decimal je = Math.Round(Convert.ToDecimal(tbcf.Rows[i]["金额"]), 2);
                //sumrow["金额"] = je.ToString("0.00");
                //sumje = sumje + je;
                //sumrow["hjid"] = tbcf.Rows[i]["hjid"];
                //tbmx.Rows.Add(sumrow);
            }
            tbmx.AcceptChanges();
            dataGridView1.DataSource = tbmx;
            dataGridView1.CurrentCell = null;


        }

        
        #region 网格的处理

        //改变行颜色
        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                foreach (DataGridViewRow dgv in dataGridView1.Rows)
                {
                    if (Convert.ToInt64(Convertor.IsNull(dgv.Cells["项目id"].Value, "0")) > 0 || (Convert.ToInt64(Convertor.IsNull(dgv.Cells["hjid"].Value, "0")) > 0 && Convertor.IsNull(dgv.Cells["序号"].Value, "0")!="小计"))
                    {
                        //dgv.DefaultCellStyle.BackColor = Color.Azure ;
                        //dgv.Cells["开嘱时间"].Style.BackColor = Color.Wheat  ;
                        dgv.Cells["医嘱内容"].Style.BackColor = Color.Wheat;
                        dgv.Cells["剂量"].Style.BackColor = Color.Wheat;
                        dgv.Cells["剂量单位"].Style.BackColor = Color.Wheat;
                        dgv.Cells["频次"].Style.BackColor = Color.Wheat;
                        dgv.Cells["用法"].Style.BackColor = Color.Wheat;
                        dgv.Cells["天数"].Style.BackColor = Color.Wheat;
                        dgv.Cells["嘱托"].Style.BackColor = Color.Wheat;
                        //dgv.Cells["开嘱医生"].Style.BackColor = Color.Wheat;
                        dgv.Cells["执行科室"].Style.BackColor = Color.Wheat;
                    }

                    //if (Convert.ToString(Convertor.IsNull(dgv.Cells["序号"].Value, "0")).Trim() == "小计")
                    //{
                    //    dgv.DefaultCellStyle.ForeColor = Color.Red;
                    //    dgv.DefaultCellStyle.Font = new System.Drawing.Font("宋体", 9, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
                    //}


                    
                    ////Convert.ToInt64(Convertor.IsNull(dgv.Cells["hjmxid"].Value, "0")) > 0 &&
                    try
                    {
                        if (dgv.Cells["修改"].Value is DBNull)
                        { }
                        else
                        {
                            if (Convert.ToBoolean(dgv.Cells["修改"].Value) == true)
                                dgv.DefaultCellStyle.ForeColor = Color.Blue;
                        }
                    }
                    catch (System.Exception err)
                    {
                        int iii = 0;
                    }
                }


            }
            catch (System.Exception err)
            {
            }
        }

        //单无格发生改变时
        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                
                if (BDelRow == true) return;
                if (dataGridView1.CurrentCell == null) return;
                DataTable tb = (DataTable)dataGridView1.DataSource;
                int nrow = dataGridView1.CurrentCell.RowIndex;
                int ncol = dataGridView1.CurrentCell.ColumnIndex;
                cell.nrow = nrow;
                cell.ncol = ncol;
                if (nrow > dataGridView1.Rows.Count) return;

                sNum = "";

                mnuAddrow.Enabled = true;
                mnuDelrow.Enabled = true;

                DataRow[] rows = tb.Select("hjid=" + Convertor.IsNull(tb.Rows[nrow]["hjid"], "0") + " ");
                //如果划价明细id=0 划价id=0 则是新处方
                if (rows.Length == 0)
                {
                    Dqcf.cfh = "0";
                    //Dqcf.ysdm = Convert.ToInt32(Convertor.IsNull(txtys.Tag, "0"));
                    //Dqcf.ksdm = Convert.ToInt32(Convertor.IsNull(txtks.Tag, "0"));
                    Dqcf.zyksid = 0;
                    Dqcf.xmly = 0;
                    Dqcf.tcid = 0;
                    Dqcf.zxksid = 0;
                    Dqcf.tjdxmdm = "";
                    Dqcf.js = 1;
                    //this.Text = Dqcf.zxksid.ToString();
                }
                else
                {
                    DataRow[] rowsx = tb.Select("hjid=" + Convertor.IsNull(tb.Rows[nrow]["hjid"], "0") + " and  执行科室id<>'0' ");
                    Dqcf.cfh = Convert.ToString(rows[0]["HJID"]);
                    //Dqcf.ysdm = Convert.ToInt32(Convertor.IsNull(rows[0]["医生id"],"0"));
                    //Dqcf.ksdm = Convert.ToInt32(Convertor.IsNull(rows[0]["科室id"],"0"));
                    //Dqcf.zyksid = Convert.ToInt32(Convertor.IsNull(rows[0]["住院科室id"],"0"));
                    Dqcf.xmly = Convert.ToInt32(Convertor.IsNull(rows[0]["项目来源"],"0"));


                    //Dqcf.tcid = Convert.ToInt64(Convertor.IsNull(rows[0]["套餐id"],"0"));
                    Dqcf.zxksid = Convert.ToInt32(Convertor.IsNull(rows[0]["执行科室id"], "0"));
                    if (rowsx.Length > 0)
                        Dqcf.zxksid = Convert.ToInt32(Convertor.IsNull(rowsx[0]["执行科室id"], "0"));
                    Dqcf.tjdxmdm = Convertor.IsNull(rows[0]["统计大项目"], "");
                    Dqcf.js = Convert.ToInt32(Convertor.IsNull(rows[0]["剂数"], "0"));

                    if (this.dataGridView1.Columns[ncol].Name == "剂量单位" && Dqcf.xmly==1)
                    {
                        if (tb.Rows[nrow]["项目id"].ToString() == "") return;

                        int cjid=Convert.ToInt32(Convertor.IsNull(tb.Rows[nrow]["项目id"],"0"));
                        string dw = tb.Rows[nrow]["剂量单位"].ToString();
                        string ssql = "select hldw id,dbo.fun_yp_ypdw(hldw) name from vi_yp_ypcd where ggid="+cjid+
                                       " union all "+
                                       " select bzdw id,dbo.fun_yp_ypdw(bzdw) name from vi_yp_ypcd  where ggid=" + cjid + "";
                        DataTable tbdw = InstanceForm.BDatabase.GetDataTable(ssql);
                        input_dw.Visible = true;
                        input_dw.Show();
                        input_dw.DataSource = tbdw;
                        input_dw.DisplayMember = "name";
                        input_dw.ValueMember = "id";
                        input_dw.Top = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Top + dataGridView1.Top;
                        input_dw.Left = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Left + dataGridView1.Left;
                        input_dw.Width = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Width;
                        //input_dw.Focus();
                        //input_dw.Text = dw;
                        
                        
                        //input_dw.DroppedDown = true;
                        
                    }
                    else
                        input_dw.Visible = false;
                }
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ModifCfje(DataTable tb, string hjid)
        {
            //修改小计
            decimal sumje = 0;
            if (hjid == "") hjid = Convertor.IsNull(hjid, "0");
            DataRow[] rows = tb.Select("hjid=" + hjid + " and 序号='小计' ");
            sumje = Convert.ToDecimal(Convertor.IsNull(tb.Compute("sum(金额)", "序号<>'小计'  and hjid=" + hjid + " "), "0"));
            if (rows.Length == 1) rows[0]["金额"] = sumje.ToString("0.00");
            DataRow[] rows1 = tb.Select("hjid=" + hjid + " and 序号<>'小计' ");

            int x = 0;
            for (int i = 0; i <= tb.Rows.Count - 1; i++)
            {
                if (hjid == Convertor.IsNull(tb.Rows[i]["hjid"], "0") && tb.Rows[i]["序号"].ToString().Trim() != "小计")
                {
                    x = x + 1;
                    tb.Rows[i]["序号"] =" "+ x.ToString();
                    tb.Rows[i]["排序序号"] = x.ToString();
                    if (tb.Rows[i]["hjmxid"].ToString() != "")
                        tb.Rows[i]["修改"] = true;
                }
            }
        }

        //网格行处理事件
        private void dataGridView1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (dataGridView1.CurrentCell == null) return;
            int nrow = dataGridView1.CurrentCell.RowIndex;
            int ncol = dataGridView1.CurrentCell.ColumnIndex;
            DataTable tb = (DataTable)dataGridView1.DataSource;
            string tjdxm = Convertor.IsNull(tb.Rows[nrow]["统计大项目"], "");

            if (Convert.ToInt32(e.KeyChar) != 13) return;

            if (dataGridView1.Columns[ncol].Name == "医嘱内容")
            {
                if (dataGridView1.CurrentRow.Index != 0)
                    dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentRow.Index - 1].Cells["剂量"];
                else
                    dataGridView1.CurrentCell = dataGridView1["剂量", 0];
                return;
            }

            if (dataGridView1.Columns[ncol].Name == "剂量")
            {
                DataRow[] rows1 = tb.Select("hjid='" + Convert.ToInt64(Convertor.IsNull(tb.Rows[nrow]["hjid"], "0")) + "' and 项目ID>0");
                if (Dqcf.tjdxmdm != "03" || rows1.Length == 1)
                {
                    if (dataGridView1.CurrentRow.Index != 0)
                        dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentRow.Index - 1].Cells["频次"];
                    else
                        dataGridView1.CurrentCell = dataGridView1["频次", 0];
                }
                else
                {
                    if (dataGridView1.CurrentCell.RowIndex == tb.Rows.Count - 1 && tb.Rows[nrow]["项目id"].ToString() != "")
                    {
                        DataRow row = tb.NewRow();
                        row["修改"] = true;
                        tb.Rows.Add(row);
                        dataGridView1.DataSource = tb;
                    }
                    dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["医嘱内容"];
                    return;
                }

                return;
            }

            if (dataGridView1.Columns[ncol].Name == "剂量单位")
            {
                if (dataGridView1.CurrentRow.Index != 0)
                    dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentRow.Index - 1].Cells["频次"];
                else
                    dataGridView1.CurrentCell = dataGridView1["频次", 0];
                return;
            }

            if (dataGridView1.Columns[ncol].Name == "频次")
            {
                if (dataGridView1.CurrentRow.Index != 0)
                    dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentRow.Index - 1].Cells["用法"];
                else
                    dataGridView1.CurrentCell = dataGridView1["用法", 0];
                return;
            }

            if (dataGridView1.Columns[ncol].Name == "用法")
            {
                if (tjdxm != "03")
                {
                    if (dataGridView1.CurrentRow.Index != 0)
                        dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentRow.Index - 1].Cells["天数"];
                    else
                        dataGridView1.CurrentCell = dataGridView1["天数", 0];
                }
                else
                {
                    if (dataGridView1.CurrentRow.Index != 0)
                        dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentRow.Index - 1].Cells["剂数"];
                    else
                        dataGridView1.CurrentCell = dataGridView1["剂数", 0];
                }
                return;
            }

            if (dataGridView1.Columns[ncol].Name == "天数" || dataGridView1.Columns[ncol].Name == "剂数")
            {
                if (dataGridView1.CurrentCell.RowIndex == tb.Rows.Count - 1 && tb.Rows[nrow]["项目id"].ToString()!="")
                {
                    DataRow row = tb.NewRow();
                    row["修改"] = true;
                    tb.Rows.Add(row);
                    dataGridView1.DataSource = tb;
                }
                dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["医嘱内容"];
                return;
            }
        }

        //网格右键菜单的可见性
        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            try
            {
                mnuDelrow.Enabled = true;
                mnuAddrow.Enabled = true;
                mnugroup.Enabled = true;
                mnuqxgroup.Enabled = true;
                mnuypzb.Enabled = true;
                mnuqxypzb.Enabled = true;

                if (dataGridView1.CurrentCell == null)
                {
                    mnuDelrow.Enabled = false;
                    mnuAddrow.Enabled = false;
                    mnugroup.Enabled = false;
                    mnuqxgroup.Enabled = false;
                    mnuypzb.Enabled = false;
                    mnuqxypzb.Enabled = false;
                    return;
                }
                DataTable tb = (DataTable)dataGridView1.DataSource;
                int nrow = dataGridView1.CurrentCell.RowIndex;
                if (nrow > dataGridView1.Rows.Count) return;

                int xmly = Convert.ToInt32(Convertor.IsNull(tb.Rows[nrow]["项目来源"], "0"));
                int sfbz = 0;

                if (xmly > 1)
                {
                    mnugroup.Enabled = false;
                    mnuqxgroup.Enabled = false;
                    mnuypzb.Enabled = false;
                    mnuqxypzb.Enabled = false;
                }

            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //当网格丢失焦点时
        private void dataGridView1_Leave(object sender, EventArgs e)
        {
            dataGridView1.CurrentCell = null;
        }

        //点击事件
        private void dataGridView1_Click(object sender, EventArgs e)
        {
            if (this.dataGridView1.CurrentCell == null) return;
            DataTable tb = (DataTable)this.dataGridView1.DataSource;
            int nrow = this.dataGridView1.CurrentCell.RowIndex;
            int ncol = this.dataGridView1.CurrentCell.ColumnIndex;
            if (dataGridView1.Columns[ncol].Name == "选择")
            {
                DataRow[] rows1 = tb.Select("hjid='" + tb.Rows[nrow]["hjid"] + "'");
                int b = Convert.ToInt16(Convertor.IsNull(tb.Rows[nrow]["选择"], "0"));
                if (b == 1)
                {
                    for (int i = 0; i <= rows1.Length - 1; i++)
                    {
                        if (rows1[i]["序号"].ToString().Trim() != "小计") rows1[i]["选择"] = false;
                    }
                }
                else
                    for (int i = 0; i <= rows1.Length - 1; i++)
                        if (rows1[i]["序号"].ToString().Trim() != "小计") rows1[i]["选择"] = true;
            }

            //txtinput.Top = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Top + dataGridView1.Top;
            //txtinput.Left = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Left + dataGridView1.Left;
            //txtinput.Width = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Width;
            //txtinput.Height = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Height;
            //txtinput.Text = tb.Rows[nrow][dataGridView1.Columns[ncol].Name].ToString(); 
            //txtinput.Focus();
            //panel_input.Top = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Top + dataGridView1.Top + txtinput.Height;
            //panel_input.Left = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Left + dataGridView1.Left;

        }

        //键盘按下事件
        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell == null) return;
                DataTable tb = (DataTable)dataGridView1.DataSource;
                int nrow = dataGridView1.CurrentCell.RowIndex;
                int ncol = dataGridView1.CurrentCell.ColumnIndex;
                // || e.KeyValue == 13
                if ((e.KeyValue >= 0 && e.KeyValue <= 9) || (e.KeyValue >= 48 && e.KeyValue <= 57) || (e.KeyValue >= 65 && e.KeyValue <= 90) ||
                    e.KeyValue == 46 || e.KeyValue == 8 || e.KeyValue == 32 || e.KeyValue == 190 || (e.KeyValue >= 96 && e.KeyValue <= 105) || e.KeyValue == 110 || e.KeyValue == 190)
                {
                }
                else
                {
                    return;
                }

                if (tb.Rows[nrow]["项目id"].ToString() == "" && dataGridView1.Columns[ncol].Name != "医嘱内容") return;
                


                #region 医嘱内容
                if (dataGridView1.Columns[ncol].Name == "医嘱内容")
                {
                    if (e.KeyValue >= 112 && e.KeyValue <= 123) return;
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Space) return;

                    if (tb.Rows[nrow]["序号"].ToString().Trim() == "小计") return;

                    //对于新处方初始化结构
                    DataRow[] rows = tb.Select("hjid=" + Convertor.IsNull(tb.Rows[nrow]["hjid"], "0") + " ");
                    //if (Convertor.IsNull(Dqcf.cfh, "0") == "0")
                    if (rows.Length == 0)
                    {

                        Dqcf.ysdm = InstanceForm.BCurrentUser.EmployeeId;
                        Dqcf.ksdm = InstanceForm.BCurrentDept.DeptId;
                        Dqcf.zyksid = 0;//隶属于住院科室收入

                        //if (_menuTag.Function_Name.Trim() == "Fun_ts_mz_sf")
                        //{
                            Dqcf.xmly = Xmly;//项目来源
                            Dqcf.zxksid = 0;//执行科室
                        //}
                        //else
                        //{
                        //    Dqcf.xmly = 1;//项目来源
                        //    Dqcf.zxksid = InstanceForm.BCurrentDept.DeptId;//执行科室
                        //}

                        Dqcf.tcid = 0;
                        Dqcf.fpcode = "";
                        Dqcf.tjdxmdm = "";
                        Dqcf.js = 1;

                    }
                    f._all = 0;
                    f._xmly = Dqcf.xmly;
                    f._zyyf = rdozyyf.Checked == true ? 1 : 0; //是否查询住院药房
                    f._execdept = Dqcf.zxksid;
                    f._deptid = InstanceForm.BCurrentDept.DeptId;
                    f.txtinput.Text = Convert.ToString((char)e.KeyCode);
                    f.txtinput.Select(1, 0);
                    f.ShowDialog();

                    if (f.ReturnRow == null) return;

                    Addrow(f.ReturnRow,ref nrow);

                    //////如果单价为零则可以输入单价
                    ////if (Convert.ToDecimal(f.ReturnRow["单价"]) == 0)
                    ////{
                    ////    tb.Rows[nrow]["单价"] = "";
                    ////    dataGridView1.CurrentCell = dataGridView1["单价", nrow];
                    ////    return;
                    ////}

                    if (dataGridView1.CurrentCell.RowIndex == tb.Rows.Count-1  && tb.Rows[nrow]["项目id"].ToString() != "")
                    {
                        DataRow row = tb.NewRow();
                        row["修改"] = true;
                       
                        tb.Rows.Add(row);
                        dataGridView1.DataSource = tb;
                    }

                    dataGridView1.CurrentCell = dataGridView1["剂量", nrow];

                    ModifCfje(tb, tb.Rows[nrow]["hjid"].ToString());
                    
                    return;
                }
                #endregion

                string KeyValue = "";
                if (e.KeyValue >= 96 && e.KeyValue <= 105)
                {
                    KeyValue = Convert.ToString(e.KeyValue - 96);
                }
                else if (e.KeyValue == 110 || e.KeyValue == 190)
                    KeyValue = ".";
                else
                    KeyValue = Convert.ToString((char)e.KeyCode);


                #region 频次
                txtinput.Tag = "";
                decimal price = Convert.ToDecimal(Convertor.IsNull(tb.Rows[nrow]["单价"], "0"));
                int  xmly = Convert.ToInt32(Convertor.IsNull(tb.Rows[nrow]["项目来源"], "0"));
                string  tjdxmdm = Convert.ToString(Convertor.IsNull(tb.Rows[nrow]["统计大项目"], ""));


                if (dataGridView1.Columns[ncol].Name == "频次" || dataGridView1.Columns[ncol].Name == "剂量"
                    || dataGridView1.Columns[ncol].Name == "用法" || (dataGridView1.Columns[ncol].Name == "单价" && xmly==2 && price==0)
                    || (dataGridView1.Columns[ncol].Name == "剂数" && tjdxmdm=="03")
                    || dataGridView1.Columns[ncol].Name == "嘱托" || (dataGridView1.Columns[ncol].Name == "天数" && tjdxmdm != "03")
                    )
                {
                    string ss = KeyValue;

                    if (KeyValue != "\b")
                    {
                        if (tb.Rows[nrow]["项目id"].ToString().Trim() == "") return;
                        sNum =KeyValue;
                    }
                    if (KeyValue == "\b" && tb.Rows[nrow][dataGridView1.Columns[ncol].Name].ToString().Length > 0)
                    {
                        sNum = tb.Rows[nrow][dataGridView1.Columns[ncol].Name].ToString();
                        sNum = sNum.ToString().Substring(0, sNum.ToString().Length - 1);
                    }

                    txtinput.Top = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Top + dataGridView1.Top;
                    txtinput.Left = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Left + dataGridView1.Left;
                    txtinput.Width = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Width;
                    txtinput.Height = dataGridView1.GetCellDisplayRectangle(ncol, nrow, true).Height;
                    txtinput.Visible = true;
                    txtinput.Text = sNum;
                    txtinput.Tag = dataGridView1.Columns[ncol].Name;
                    txtinput.Focus();
                    txtinput.Select(txtinput.Text.Length, 0);
                    return;
                }
                #endregion

            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        //文本框输入
        private void txtinput_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                int key = Convert.ToInt32(e.KeyData);
                DataTable tb = (DataTable)this.dataGridView1.DataSource;
                DataTable tb_temp;
                if (e.KeyData == Keys.Down)
                {
                    int i = dataGridView2.Rows.GetNextRow(dataGridView2.CurrentRow.Index, DataGridViewElementStates.None); //获取原选定下一行索引
                    if (i ==- 1) return;
                    dataGridView2.CurrentCell = dataGridView2[1, i]; //指针下移
                    dataGridView2.Rows[i].Selected = true; //选中整行

                }
                if (e.KeyData == Keys.Up)
                {
                    txtinput.Select(txtinput.Text.Trim().Length, 0);
                    int i = dataGridView1.Rows.GetPreviousRow(dataGridView2.CurrentRow.Index, DataGridViewElementStates.None); //获取原选定下一行索引
                    if (i == -1) return;
                    dataGridView2.CurrentCell = dataGridView2[1, i]; //指针上移
                    dataGridView2.Rows[i].Selected = true; //选中整行
                }
                

                if (dataGridView1.Columns[cell.ncol].Name == "剂量" && key == 13)
                {
                    if (Convertor.IsNumeric(txtinput.Text) == false) { MessageBox.Show("剂量必须输入数字", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    tb.Rows[cell.nrow]["剂量"] = txtinput.Text;
                    txtinput.Text = "";
                    txtinput.Visible = false;
                    Seek_Price(tb.Rows[cell.nrow]);
                    ModifCfje(tb, tb.Rows[cell.nrow]["hjid"].ToString());
                    //dataGridView1.CurrentCell = dataGridView1["频次",cell.nrow];
                    DataRow[] rows1 = tb.Select("hjid='" + Convert.ToInt64(Convertor.IsNull(tb.Rows[cell.nrow]["hjid"], "0")) + "' and 项目ID>0");
                    if (cell.nrow == tb.Rows.Count - 1 && tb.Rows[cell.nrow]["项目id"].ToString() != "")
                    {
                        DataRow row = tb.NewRow();
                        row["修改"] = true;
                        
                        tb.Rows.Add(row);
                        dataGridView1.DataSource = tb;
                    }
                    else
                    {
                        if (Dqcf.tjdxmdm == "03" && rows1.Length>1)
                            dataGridView1.CurrentCell = dataGridView1["医嘱内容", cell.nrow+1 ];
                        else
                            dataGridView1.CurrentCell = dataGridView1["频次", cell.nrow ];
                    }
                    dataGridView1.Focus();
                    return;
                }

                if (dataGridView1.Columns[cell.ncol].Name == "天数" && key == 13)
                {
                    if (Convertor.IsNumeric(txtinput.Text) == false) { MessageBox.Show("天数必须输入数字", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    tb.Rows[cell.nrow]["天数"] = txtinput.Text;
                    txtinput.Text = "";
                    txtinput.Visible = false;
                    Seek_Price(tb.Rows[cell.nrow]);
                    ModifCfje(tb, tb.Rows[cell.nrow]["hjid"].ToString());
                    if (cell.nrow == tb.Rows.Count - 1 && tb.Rows[cell.nrow]["项目id"].ToString() != "")
                    {
                        DataRow row = tb.NewRow();
                        row["修改"] = true;
                       
                        tb.Rows.Add(row);
                        dataGridView1.DataSource = tb;
                    }
                    else
                    {
                        dataGridView1.CurrentCell = dataGridView1["医嘱内容", cell.nrow+1];
                    }
                    dataGridView1.Focus();
                    return;
                }

                if (dataGridView1.Columns[cell.ncol].Name == "频次" )
                {
                    if (e.KeyData != Keys.Up && e.KeyData != Keys.Down && e.KeyData!=Keys.Enter)
                    {
                        DataRow[] rows = _dataSet.Tables["pc"].Select(" pym LIKE '" + txtinput.Text + "%'");
                        tb_temp = _dataSet.Tables["pc"].Clone();
                        for (int i = 0; i <= rows.Length - 1; i++)
                            tb_temp.ImportRow(rows[i]);
                        dataGridView2.DataSource = tb_temp;
                        dataGridView2.Visible = true;
                        panel_input.Top = dataGridView1.GetCellDisplayRectangle(cell.ncol, cell.nrow, true).Top + dataGridView1.Top + txtinput.Height;
                        panel_input.Left = dataGridView1.GetCellDisplayRectangle(cell.ncol, cell.nrow, true).Left + dataGridView1.Left;
                        panel_input.Width = 80;
                        panel_input.Visible = true;
                    }
                    if (e.KeyData == Keys.Enter && dataGridView2.CurrentRow!=null)
                    {
                        if (dataGridView2.CurrentRow.Index >= 0)
                        {
                            DataTable tbx = (DataTable)this.dataGridView2.DataSource;
                            tb.Rows[cell.nrow]["频次"] = tbx.Rows[dataGridView2.CurrentRow.Index]["name"];
                            tb.Rows[cell.nrow]["频次id"] = tbx.Rows[dataGridView2.CurrentRow.Index]["id"];
                            Seek_Price(tb.Rows[cell.nrow]);
                            ModifCfje(tb, tb.Rows[cell.nrow]["hjid"].ToString());
                            panel_input.Visible = false;
                            txtinput.Visible = false;
                            if (Convertor.IsNull(tb.Rows[cell.nrow]["用法"],"")=="")
                                 this.dataGridView1.CurrentCell = dataGridView1["用法", cell.nrow];
                            else
                                 this.dataGridView1.CurrentCell = dataGridView1["天数", cell.nrow];
                            dataGridView1.Focus();
                        }
                    }
                    return;
                }

                if (dataGridView1.Columns[cell.ncol].Name == "用法")
                {
                    if (e.KeyData != Keys.Up && e.KeyData != Keys.Down && e.KeyData != Keys.Enter)
                    {
                        DataRow[] rows = _dataSet.Tables["yf"].Select(" pym LIKE '" + txtinput.Text + "%'");
                        tb_temp = _dataSet.Tables["yf"].Clone();
                        for (int i = 0; i <= rows.Length - 1; i++)
                            tb_temp.ImportRow(rows[i]);
                        dataGridView2.DataSource = tb_temp;
                        dataGridView2.Visible = true;
                        panel_input.Top = dataGridView1.GetCellDisplayRectangle(cell.ncol, cell.nrow, true).Top + dataGridView1.Top + txtinput.Height;
                        panel_input.Left = dataGridView1.GetCellDisplayRectangle(cell.ncol, cell.nrow, true).Left + dataGridView1.Left;
                        panel_input.Width = 120;
                        this.input_name.Width = 30;
                        panel_input.Visible = true;
                    }
                    if (e.KeyData == Keys.Enter && dataGridView2.CurrentRow != null  )
                    {
                        if (dataGridView2.CurrentRow.Index >= 0)
                        {
                            DataTable tbx = (DataTable)this.dataGridView2.DataSource;
                            tb.Rows[cell.nrow]["用法"] = tbx.Rows[dataGridView2.CurrentRow.Index]["name"];
                            tb.Rows[cell.nrow]["用法id"] = tbx.Rows[dataGridView2.CurrentRow.Index]["id"];
                            tb.Rows[cell.nrow]["修改"] = true;
                            panel_input.Visible = false;
                            txtinput.Visible = false;
                            if (Dqcf.tjdxmdm!="03")
                                this.dataGridView1.CurrentCell = dataGridView1["天数", cell.nrow];
                            else
                                this.dataGridView1.CurrentCell = dataGridView1["剂数", cell.nrow];
                            dataGridView1.Focus();
                        }
                    }
                    return;
                }

                if (dataGridView1.Columns[cell.ncol].Name == "嘱托" && key == 13)
                {
                    tb.Rows[cell.nrow]["嘱托"] = txtinput.Text;
                    txtinput.Text = "";
                    txtinput.Visible = false;
                    dataGridView1.CurrentCell = dataGridView1["嘱托", cell.nrow];
                    dataGridView1.Focus();
                    return;
                }

                if (dataGridView1.Columns[cell.ncol].Name == "剂数" && key == 13)
                {
                    if (Convertor.IsNumeric(txtinput.Text) == false) { MessageBox.Show("剂数必须输入数字", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    tb.Rows[cell.nrow]["剂数"] = Convert.ToInt32(txtinput.Text).ToString();
                    txtinput.Text = "";
                    txtinput.Visible = false;
                    DataRow[] rows1 = tb.Select("hjid='" + Convert.ToInt64(Convertor.IsNull(tb.Rows[cell.nrow]["hjid"], "0")) + "' and 项目ID>0");
                    for (int i = 0; i <= rows1.Length - 1; i++)
                    {
                        rows1[i]["剂数"] = tb.Rows[cell.nrow]["剂数"];
                        Seek_Price(rows1[i]);
                    }
                    ModifCfje(tb, Convertor.IsNull(tb.Rows[cell.nrow]["hjid"], "0"));
                    if (cell.nrow == tb.Rows.Count - 1 && tb.Rows[cell.nrow]["项目id"].ToString() != "")
                    {
                        DataRow row = tb.NewRow();
                        row["修改"] = true;
                        
                        tb.Rows.Add(row);
                        dataGridView1.DataSource = tb;
                    }
                    else
                    {
                        dataGridView1.CurrentCell = dataGridView1["医嘱内容", cell.nrow + 1];
                    }
                    dataGridView1.Focus();
                    return;
                }


                if (dataGridView1.Columns[cell.ncol].Name == "单价" && key == 13)
                {
                    long tcid = Convert.ToInt64(Convertor.IsNull(tb.Rows[cell.nrow]["套餐ID"],"0"));
                    if (tcid > 0) { MessageBox.Show("套餐不能修改价格", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error); txtinput.Text = ""; txtinput.Visible = false; return; }
                    if (Convertor.IsNumeric(txtinput.Text) == false) { MessageBox.Show("单价必须输入数字", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    tb.Rows[cell.nrow]["单价"] = Convert.ToDecimal(txtinput.Text).ToString();
                    txtinput.Text = "";
                    txtinput.Visible = false;
                    Seek_Price(tb.Rows[cell.nrow]);
                    ModifCfje(tb, Convertor.IsNull(tb.Rows[cell.nrow]["hjid"], "0"));
                    if (cell.nrow == tb.Rows.Count - 1 && tb.Rows[cell.nrow]["项目id"].ToString() != "")
                    {
                        DataRow row = tb.NewRow();
                        row["修改"] = true;
                        
                        tb.Rows.Add(row);
                        dataGridView1.DataSource = tb;
                    }
                    else
                    {
                        dataGridView1.CurrentCell = dataGridView1["医嘱内容", cell.nrow + 1];
                    }
                    dataGridView1.Focus();
                    return;
                }

            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }
        //文本丢失焦点事件
        private void txtinput_Leave(object sender, EventArgs e)
        {
            txtinput.Visible = false;//if (dataGridView2.Visible == false)
           panel_input.Visible = false;
        }
        //检索网格
        private void dataGridView2_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (txtinput.Visible == false && Convertor.IsNull(txtinput.Tag, "") == "") return;
                DataTable tb = (DataTable)this.dataGridView1.DataSource;
                DataTable tb2 = (DataTable)this.dataGridView2.DataSource;
                if (tb2 == null) return;
                if (this.dataGridView2.CurrentCell == null) return;
                int nkey = Convert.ToInt32(e.KeyChar);
                if (Convertor.IsNull(txtinput.Tag, "") == "频次" && nkey == 13)
                {
                    tb.Rows[cell.nrow]["频次"] = tb2.Rows[this.dataGridView2.CurrentCell.RowIndex]["name"];
                    tb.Rows[cell.nrow]["频次id"] = tb2.Rows[this.dataGridView2.CurrentCell.RowIndex]["id"];
                    txtinput.Visible = false;
                    panel_input.Visible = false;
                    this.dataGridView1.CurrentCell = dataGridView1["用法", cell.nrow];
                }
                if (Convertor.IsNull(txtinput.Tag, "") == "用法" && nkey == 13)
                {
                    tb.Rows[cell.nrow]["用法"] = tb2.Rows[this.dataGridView2.CurrentCell.RowIndex]["name"];
                    tb.Rows[cell.nrow]["用法id"] = tb2.Rows[this.dataGridView2.CurrentCell.RowIndex]["id"];
                    txtinput.Visible = false;
                    panel_input.Visible = false;
                    this.dataGridView1.CurrentCell = dataGridView1["天数", cell.nrow];
                }
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        //单位选择事件
        private void input_dw_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                DataTable tb = (DataTable)this.dataGridView1.DataSource;
                if (Convert.ToInt32(e.KeyChar) != 13) return;
                if (input_dw.SelectedIndex < 0) return;
                tb.Rows[cell.nrow]["剂量单位"] = input_dw.Text;
                tb.Rows[cell.nrow]["剂量单位id"] = Convertor.IsNull(input_dw.SelectedValue, "");
                tb.Rows[cell.nrow]["dwlx"] = Convert.ToString(input_dw.SelectedIndex + 1);
                Seek_Price(tb.Rows[cell.nrow]);
                input_dw.Visible = false;
                this.dataGridView1.CurrentCell = dataGridView1[ "频次",cell.nrow];
                this.dataGridView1.Focus();
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 菜单事件
        //新增行
        private void mnuAddrow_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null) return;
            DataTable tb = (DataTable)dataGridView1.DataSource;
            int nrow = dataGridView1.CurrentCell.RowIndex;
            if (nrow > tb.Rows.Count) return;
            DataRow row = tb.NewRow();
            row["HJID"] = Dqcf.cfh;
            row["修改"] = true;
            
            tb.Rows.InsertAt(row, nrow);
            dataGridView1.DataSource = tb;
            dataGridView1.CurrentCell = dataGridView1.Rows[nrow].Cells["医嘱内容"];
            dataGridView1.Focus();
        }
        //删除行
        private void mnuDelrow_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null) return;
            BDelRow = true;
            DataTable tb = (DataTable)dataGridView1.DataSource;
            int nrow = dataGridView1.CurrentCell.RowIndex;
            if (nrow > tb.Rows.Count) return;
            long hjmxid = Convert.ToInt64(Convertor.IsNull(tb.Rows[nrow]["hjmxid"], "0"));
            long hjid = Convert.ToInt64(Convertor.IsNull(tb.Rows[nrow]["hjid"], "0"));
            //long tcid = Convert.ToInt64(Convertor.IsNull(tb.Rows[nrow]["套餐id"], "0"));
            //long order_id = Convert.ToInt64(Convertor.IsNull(tb.Rows[nrow]["yzid"], "0"));
            string ssql = "";
            if (hjmxid == 0)
            {
                try
                {
                    DataRow row = tb.Rows[nrow];
                    tb.Rows.Remove(row);
                    ModifCfje(tb, hjid.ToString());
                    //ComputerJE(tb, hjid.ToString());//Modify By Tany 2009-01-05
                    BDelRow = false;
                    return;
                }
                catch (System.Exception err)
                {
                    MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }


            try
            {
                InstanceForm.BDatabase.BeginTransaction();

                tb.Rows.RemoveAt(nrow);
                jc_mb.Delete_Mbmx(hjmxid);

                ssql = "select * from jc_cfmb_mx where mbid=" + hjid + "";
                DataTable tab = InstanceForm.BDatabase.GetDataTable(ssql);
                if (tab.Rows.Count == 0)
                {
                    jc_mb.Delete_Mb(hjid);
                }
                InstanceForm.BDatabase.CommitTransaction();

                Select_Mb();
                DataTable tbmbmx = Read_Mb(hjid);
                AddPresc(tbmbmx);

                ModifCfje(tb, hjid.ToString());
                
                BDelRow = false;

            }
            catch (System.Exception err)
            {
                BDelRow = false;
                InstanceForm.BDatabase.RollbackTransaction();
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }



        }
        //分组处方
        private void mnugroup_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable tb = (DataTable)dataGridView1.DataSource ;
                DataTable myTb = new DataTable();
                myTb.Columns.Add("nrow", Type.GetType("System.Int32"));
                for (int i=0;i<=dataGridView1.SelectedCells.Count- 1;i++)
                {
                    DataRow[] rows= myTb.Select("nrow=" + dataGridView1.SelectedCells[i].RowIndex + "");
                    long xmid = Convert.ToInt64(Convertor.IsNull(tb.Rows[dataGridView1.SelectedCells[i].RowIndex]["项目id"], "0"));
                    if (rows.Length == 0 && xmid>0)
                    {
                        DataRow row = myTb.NewRow();
                        row["nrow"] = dataGridView1.SelectedCells[i].RowIndex;
                        myTb.Rows.Add(row);
                    }
                }

                if (myTb.Rows.Count <= 1) { return; }
                bool b = false;
                DataRow[] rowsX = myTb.Select("", "nrow");
                for (int i = 0; i <= rowsX.Length - 1; i++)
                {
                    int nrow = Convert.ToInt32(rowsX[i]["nrow"]);
                    int fzxh = Convert.ToInt32(Convertor.IsNull(tb.Rows[nrow]["处方分组序号"], "0"));
                    if (fzxh != 0) { b = true; break; }
                }
                if (b == true) { MessageBox.Show("选择的行可能已被包含在其它分组中,请重新选择", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                for (int i = 0; i <= rowsX.Length - 1; i++)
                {
                    int nrow = Convert.ToInt32(rowsX[i]["nrow"]);
                    
                    if (i == 0) { tb.Rows[nrow]["处方分组序号"] = "1"; tb.Rows[nrow]["医嘱内容"] = "┌" + tb.Rows[nrow]["医嘱内容"].ToString(); }
                    if (i == rowsX.Length - 1) { tb.Rows[nrow]["处方分组序号"] = "-1"; tb.Rows[nrow]["医嘱内容"] = "└" + tb.Rows[nrow]["医嘱内容"].ToString(); }
                    if (i != 0 && i != rowsX.Length - 1) { tb.Rows[nrow]["处方分组序号"] = "0"; tb.Rows[nrow]["医嘱内容"] = "│" + tb.Rows[nrow]["医嘱内容"].ToString(); }

                    long hjmxid = Convert.ToInt64(Convertor.IsNull(tb.Rows[nrow]["hjmxid"], "0"));
                    long cffzxh = Convert.ToInt32(Convertor.IsNull(tb.Rows[nrow]["处方分组序号"], "0"));
                    if (hjmxid > 0)
                    {
                        string ssql = "update jc_cfmb_mx set fzxh=" + cffzxh + " where mbmxid=" + hjmxid + "";
                        InstanceForm.BDatabase.DoCommand(ssql);
                    }
                }
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message,"错误",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        //取消分组处方
        private void mnuqxgroup_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable tb = (DataTable)this.dataGridView1.DataSource;
                if (dataGridView1.CurrentCell == null) return;
                int nrow = dataGridView1.CurrentCell.RowIndex;
                DataRow[] rows = tb.Select("hjid=" + Convertor.IsNull(tb.Rows[nrow]["hjid"], "0") + "");
                for (int i = 0; i <= rows.Length - 1; i++)
                {
                    string s = rows[i]["医嘱内容"].ToString();
                    s = s.Replace("┌", ""); s = s.Replace("│", ""); s = s.Replace("└", "");
                    rows[i]["医嘱内容"] = s;
                    rows[i]["处方分组序号"] = "0";

                    long hjmxid = Convert.ToInt64(Convertor.IsNull(rows[i]["hjmxid"], "0"));
                    long cffzxh = Convert.ToInt32(Convertor.IsNull(rows[i]["处方分组序号"], "0"));
                    if (hjmxid > 0)
                    {
                        string ssql = "update jc_cfmb_mx set fzxh=" + cffzxh + " where mbmxid=" + hjmxid + "";
                        InstanceForm.BDatabase.DoCommand(ssql);
                    }
                }
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message,"错误",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }
        }
        //药品自备
        private void mnuypzb_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable tb = (DataTable)dataGridView1.DataSource;
                if (dataGridView1.CurrentCell == null) return;
                int nrow = this.dataGridView1.CurrentCell.RowIndex;

                if (Convert.ToInt32(Convertor.IsNull(tb.Rows[nrow]["项目来源"], "0")) > 1)
                {
                    MessageBox.Show("非药品不能进行这种操作", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }


                if (tb.Rows[nrow]["医嘱内容"].ToString().Contains("【自备】") == true)
                {
                    MessageBox.Show("该药品已经是自备药,不能重复指定", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                tb.Rows[nrow]["医嘱内容"] = tb.Rows[nrow]["医嘱内容"] + " 【自备】";
                tb.Rows[nrow]["自备药"] = "1";
                tb.Rows[nrow]["修改"] = "1";
               
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message,"",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        //取消药品自备
        private void mnuqxypzb_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable tb = (DataTable)dataGridView1.DataSource;
                if (dataGridView1.CurrentCell == null) return;
                int nrow = this.dataGridView1.CurrentCell.RowIndex;

                if (Convert.ToInt32(Convertor.IsNull(tb.Rows[nrow]["项目来源"], "0")) > 1)
                {
                    MessageBox.Show("非药品不能进行这种操作", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }


                if (tb.Rows[nrow]["医嘱内容"].ToString().Contains("【自备】") == true)
                {
                    string yznr=tb.Rows[nrow]["医嘱内容"].ToString().Replace("【自备】","");
                    tb.Rows[nrow]["医嘱内容"] = yznr.ToString();
                    tb.Rows[nrow]["自备药"] = "0";
                    tb.Rows[nrow]["修改"] = "1";
                }



            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnudelmb_Click(object sender, EventArgs e)
        {
            try
            {
                ListViewItem item = (ListViewItem)listView_mb.SelectedItems[0];
                if (item == null) return;
                string mbid = item.SubItems["mbid"].Text;

                jc_mb.Delete_Mb(Convert.ToInt64(mbid));

                MessageBox.Show("删除成功");
                Select_Mb();
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "修改模板名", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion


        #region 右边工具栏

        // 保存处方
        private void butsave_Click(object sender, EventArgs e)
        {
            DataTable tb = (DataTable)dataGridView1.DataSource;
            try
            {
                if (butsave.Enabled == false) return;


                //分组处方
                string[] GroupbyField1 ={ "HJID", "执行科室ID"};
                string[] ComputeField1 ={ "金额" };
                string[] CField1 ={ "sum" };
                TrasenFrame.Classes.TsSet xcset1 = new TrasenFrame.Classes.TsSet();
                xcset1.TsDataTable = tb;
                DataTable tbcf1 = xcset1.GroupTable(GroupbyField1, ComputeField1, CField1, "修改=true and 项目id>0");
                if (tbcf1.Rows.Count == 0) { return; }

                string[] GroupbyField ={ "HJID", "执行科室ID", "项目来源", "剂数"};
                string[] ComputeField ={ "金额" };
                string[] CField ={ "sum" };
                TrasenFrame.Classes.TsSet xcset = new TrasenFrame.Classes.TsSet();
                xcset.TsDataTable = tb;
                DataTable tbcf = xcset.GroupTable(GroupbyField, ComputeField, CField, "修改=true and 项目id>0");
                if (tbcf.Rows.Count == 0) { return; }

                if (tbcf1.Rows.Count != tbcf.Rows.Count)
                {
                    MessageBox.Show("请检查处方的数据是否正确,可能存在同一张处方有不同的执行科室或不同的医生或不同的开单科室的情况", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (tbcf.Rows.Count > 1)
                {
                    MessageBox.Show("一次只能输入一张处方", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //返回变量
                int _err_code = -1;
                string _err_text = "";
                //时间
                string _sDate = DateManager.ServerDateTimeByDBType(InstanceForm.BDatabase).ToString();

                long _Mbid = Convert.ToInt64(Convertor.IsNull(tbcf.Rows[0]["hjid"], "0"));
                string _mbmc = "";
                string _pym = "";
                string _wbm = "";
                string _bz = "";
                int _mbjb = 0;
                int _ksdm = 0;
                int _ysdm = 0;
                int _zxks = Convert.ToInt32(Convertor.IsNull(tbcf.Rows[0]["执行科室ID"], "0"));
                string _djsj = _sDate;
                int _djy = InstanceForm.BCurrentUser.EmployeeId ;
                long _NewMbid = 0;
                int _xmly = Convert.ToInt32(tbcf.Rows[0]["项目来源"]);
                int _js = Convert.ToInt32(tbcf.Rows[0]["剂数"]);


                if (_menuTag.Function_Name == "Fun_ts_mz_xtsz_cfmb_qy") { _mbjb = 0; }
                if (_menuTag.Function_Name == "Fun_ts_mz_xtsz_cfmb_ks") { _mbjb = 1; _ksdm = InstanceForm.BCurrentDept.DeptId; }
                if (_menuTag.Function_Name == "Fun_ts_mz_xtsz_cfmb_ys") { _mbjb = 2; _ysdm = InstanceForm.BCurrentUser.EmployeeId; }

                if (_Mbid == 0)
                {
                    DlgInputBox Inputbox = new DlgInputBox("", "请输入模板名称", "保存模板");
                    Inputbox.NumCtrl = false;
                    Inputbox.ShowDialog();
                    if (!DlgInputBox.DlgResult) return;
                    _mbmc = DlgInputBox.DlgValue.ToString();
                }
                else
                {
                    string ssql = "select * from jc_cfmb where mbid=" + _Mbid + "";
                    DataTable tbmb = InstanceForm.BDatabase.GetDataTable(ssql);
                    if (tbmb.Rows.Count > 0)
                    {
                        _mbmc = tbmb.Rows[0]["mbmc"].ToString();
                        _ksdm = Convert.ToInt32(tbmb.Rows[0]["ksdm"]);
                        _ysdm = Convert.ToInt32(tbmb.Rows[0]["ysdm"]);
                        _djsj = tbmb.Rows[0]["djsj"].ToString();
                        _djy = Convert.ToInt32(tbmb.Rows[0]["djy"]);
                    }
                }
               

                InstanceForm.BDatabase.BeginTransaction();

                //查找当前处方
                DataRow[] rows = tb.Select("HJID=" + _Mbid + " and 执行科室ID=" + _zxks + "  and 修改=true and 项目id>0 ");

                jc_mb.SaveMb(_Mbid, TrasenFrame.Forms.FrmMdiMain.Jgbm, _mbmc, _pym, _wbm, _bz, _mbjb, _ksdm, _ysdm, _zxks, _djsj, _djy, out _NewMbid,out _err_code, out _err_text);
                if ((_NewMbid == 0 && _Mbid == 0) || _err_code != 0) throw new Exception(_err_text);
                
                if (rows == null) throw new Exception("没有找到行，请刷新数据");
                if (rows.Length == 0 && _Mbid > 0) throw new Exception("没有需要保存的行");
                //插处方明细表
                for (int j = 0; j <= rows.Length - 1; j++)
                {

                        #region 保存明细
                        long _NewMbmxid = 0;
                        long _mbmxid = Convert.ToInt64(Convertor.IsNull(rows[j]["hjmxid"], "0"));
                        string _pm = Convertor.IsNull(rows[j]["医嘱内容"], "");
                        decimal _dj = Convert.ToDecimal(Convertor.IsNull(rows[j]["单价"], "0"));
                        decimal _sl = Convert.ToDecimal(Convertor.IsNull(rows[j]["数量"], "0"));
                        decimal _je = Convert.ToDecimal(Convertor.IsNull(rows[j]["金额"], "0"));
                        long _xmid = Convert.ToInt64(Convertor.IsNull(rows[j]["项目id"], "0"));
                        int _bzby = Convert.ToInt32(Convertor.IsNull(rows[j]["自备药"], "0"));
                        decimal _yl = Convert.ToDecimal(Convertor.IsNull(rows[j]["剂量"], "0"));
                        string _yldw = Convertor.IsNull(rows[j]["剂量单位"], "");
                        int _yldwid = Convert.ToInt32(Convertor.IsNull(rows[j]["剂量单位id"], "0"));
                        int _dwlx = Convert.ToInt32(Convertor.IsNull(rows[j]["dwlx"], "0"));
                        int _yfid = Convert.ToInt32(Convertor.IsNull(rows[j]["用法id"], "0"));
                        int _pcid = Convert.ToInt32(Convertor.IsNull(rows[j]["频次id"], "0"));
                        decimal _ts = Convert.ToDecimal(Convertor.IsNull(rows[j]["天数"], "0"));
                        string _zt = Convert.ToString(Convertor.IsNull(rows[j]["嘱托"], ""));
                        int _fzxh = Convert.ToInt32(Convertor.IsNull(rows[j]["处方分组序号"], "0"));
                        int _pxxh = Convert.ToInt32(Convertor.IsNull(rows[j]["排序序号"], "0"));
                        int _cjid = Convert.ToInt32(Convertor.IsNull(rows[j]["cjid"], "0"));
                        //if ((_sl == 0 || _js == 0 || _je == 0) && _bzby==0) throw new Exception(_pm+" 没有数量或金额");
                        jc_mb.SaveMbmx(_mbmxid, _NewMbid, _xmid, _xmly, _yl, _yldw, _yldwid, _dwlx, _yfid, _pcid, _zt,
                            _ts, _fzxh, _pxxh, _bzby, _cjid,_js, out  _NewMbmxid, out _err_code, out _err_text,Guid.Empty,0);
                        if ((_NewMbmxid == 0 && _mbmxid == 0) || _err_code != 0) throw new Exception(_err_text);

                        #endregion 非套餐

                }

                //}

                InstanceForm.BDatabase.CommitTransaction();

                MessageBox.Show("保存成功");
                Select_Mb();
                DataTable tbmbmx = Read_Mb(_NewMbid);
                AddPresc(tbmbmx);

                //panel1.Visible = true;
                lblmbmc.Text = _mbmc;
                lbldjsj.Text = _djsj;
                lbldjy.Text = Fun.SeekEmpName(_djy);
            }
            catch (System.Exception err)
            {
                InstanceForm.BDatabase.RollbackTransaction();
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }




        }

        //新开处方按钮事件            
        private void butnew_Click(object sender, EventArgs e)
        {
            try
            {
                if (butnew.Enabled == false) return;

                //panel1.Visible = false;
                lblmbmc.Text = "";
                lbldjsj.Text = "";
                lbldjy.Text = "";

                DataTable tb = (DataTable)dataGridView1.DataSource;

                tb.Rows.Clear();
                Dqcf.mbid = 0;
                butsgmc.Enabled = false;

                int nrow = tb.Rows.Count - 1;
                if (nrow > tb.Rows.Count - 1 || nrow >= 0)
                {
                    if (Convertor.IsNull(tb.Rows[nrow]["序号"], "").Trim() != "小计")
                    {
                        Dqcf.cfh = "New";
                        dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["医嘱内容"];
                        dataGridView1.Focus();
                        return;
                    }
                }
                DataRow row = tb.NewRow();
                row["修改"] = true;
                tb.Rows.Add(row);
                dataGridView1.DataSource = tb;
                Dqcf.cfh = "New";
                ((FrmMdiMain)_mdiParent).sttbpDescription.Text = "";
                dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["医嘱内容"];
                dataGridView1.Focus();
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message);
            }

        }

        // 刷新项目
        private void butsxxm_Click(object sender, EventArgs e)
        {
            Cursor.Current = PubStaticFun.WaitCursor();
            try
            {
                DataTable tb;
                //if (_menuTag.Function_Name.Trim() == "Fun_ts_mz_hj")
                //    tb = Fun.GetXmYp_YZ(1, 0, 0, InstanceForm.BCurrentDept.DeptId, InstanceForm.BCurrentDept.DeptId, "", "", "");
                //else
                    tb = Fun.GetXmYp_YZ(1, 0, 0, 0, InstanceForm.BCurrentDept.DeptId, "", "", "",TrasenFrame.Forms.FrmMdiMain.Jgbm,0);
                tb.TableName = "ITEM";
                if (PubDset.Tables.Contains("ITEM"))
                    PubDset.Tables.Remove("ITEM");

                PubDset.Tables.Add(tb);
                f.Dset = PubDset;
                Cursor.Current = Cursors.Default;
            }
            catch (System.Exception err)
            {
                Cursor.Current = Cursors.Default;
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        #endregion

        #region 方法
        //常用药品
        private void Select_Cyyp()
        {
            try
            {
                DateTime djsj = DateManager.ServerDateTimeByDBType(InstanceForm.BDatabase);

                DataTable tb = mzys.Select_cyyp(InstanceForm.BCurrentUser.EmployeeId);

                listView_cyyp.Items.Clear();
                for (int i = 0; i <= tb.Rows.Count - 1; i++)
                {
                    ListViewItem item = new ListViewItem();
                    item.Name = "品名";
                    item.Text = Convertor.IsNull(tb.Rows[i]["品名"], "");

                    ListViewItem.ListViewSubItem subitem_gg = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["规格"], ""));
                    subitem_gg.Name = "规格";
                    item.SubItems.Add(subitem_gg);

                    ListViewItem.ListViewSubItem subitem_pym = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["拼音码"], ""));
                    subitem_pym.Name = "拼音码";
                    item.SubItems.Add(subitem_pym);

                    ListViewItem.ListViewSubItem subitem_sypc = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["频率"], ""));
                    subitem_sypc.Name = "频率";
                    item.SubItems.Add(subitem_sypc);

                    ListViewItem.ListViewSubItem subitem_ggid = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["ggid"], ""));
                    subitem_ggid.Name = "ggid";
                    item.SubItems.Add(subitem_ggid);

                    ListViewItem.ListViewSubItem subitem_cjid = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["cjid"], ""));
                    subitem_cjid.Name = "cjid";
                    item.SubItems.Add(subitem_cjid);

                    listView_cyyp.Items.Add(item);
                }
                

            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //常用项目
        private void Select_Cyxm()
        {
            try
            {
                DateTime djsj = DateManager.ServerDateTimeByDBType(InstanceForm.BDatabase);

                DataTable tb = mzys.Select_cyxm(InstanceForm.BCurrentUser.EmployeeId);

                listView_cyxm.Items.Clear();
                for (int i = 0; i <= tb.Rows.Count - 1; i++)
                {
                    ListViewItem item = new ListViewItem();
                    item.Name = "项目名称";
                    item.Text = Convertor.IsNull(tb.Rows[i]["项目名称"], "");

                    ListViewItem.ListViewSubItem subitem_dj = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["单价"], ""));
                    subitem_dj.Name = "单价";
                    item.SubItems.Add(subitem_dj);

                    ListViewItem.ListViewSubItem subitem_dw = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["单位"], ""));
                    subitem_dw.Name = "单位";
                    item.SubItems.Add(subitem_dw);

                    ListViewItem.ListViewSubItem subitem_pym = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["拼音码"], ""));
                    subitem_pym.Name = "拼音码";
                    item.SubItems.Add(subitem_pym);

                    ListViewItem.ListViewSubItem subitem_sypc = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["频率"], ""));
                    subitem_sypc.Name = "频率";
                    item.SubItems.Add(subitem_sypc);

                    ListViewItem.ListViewSubItem subitem_tcid = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["tcid"], ""));
                    subitem_tcid.Name = "tcid";
                    item.SubItems.Add(subitem_sypc);

                    ListViewItem.ListViewSubItem subitem_order_id = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["order_id"], ""));
                    subitem_order_id.Name = "order_id";
                    item.SubItems.Add(subitem_order_id);

                    listView_cyxm.Items.Add(item);
                }


            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //读取模板
        private void Select_Mb()
        {
            try
            {
                DateTime djsj = DateManager.ServerDateTimeByDBType(InstanceForm.BDatabase);

                DataTable tb = null;
                if (_menuTag.Function_Name == "Fun_ts_mz_xtsz_cfmb_qy")
                    tb = jc_mb.SelectyMb(0, 0, 0);
                else if (_menuTag.Function_Name == "Fun_ts_mz_xtsz_cfmb_ks")
                    tb = jc_mb.SelectyMb(1, InstanceForm.BCurrentDept.DeptId,InstanceForm.BCurrentUser.EmployeeId);
                else
                    tb = jc_mb.SelectyMb(2, 0, InstanceForm.BCurrentUser.EmployeeId);

                listView_mb.Items.Clear();
                for (int i = 0; i <= tb.Rows.Count - 1; i++)
                {
                    ListViewItem item = new ListViewItem();
                    item.Name = "模板名称";
                    item.Text = Convertor.IsNull(tb.Rows[i]["模板名称"], "");

                    ListViewItem.ListViewSubItem subitem_zxks = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["执行科室"], ""));
                    subitem_zxks.Name = "执行科室";
                    item.SubItems.Add(subitem_zxks);

                    ListViewItem.ListViewSubItem subitem_jb = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["级别"], ""));
                    subitem_jb.Name = "级别";
                    item.SubItems.Add(subitem_jb);

                    ListViewItem.ListViewSubItem subitem_mbid = new ListViewItem.ListViewSubItem(item, Convertor.IsNull(tb.Rows[i]["mbid"], ""));
                    subitem_mbid.Name = "mbid";
                    item.SubItems.Add(subitem_mbid);

                    listView_mb.Items.Add(item);
                }


            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //读取某个模板
        private DataTable Read_Mb(long mbid)
        {
            try
            {
                ParameterEx[] parameters = new ParameterEx[1];

                parameters[0].Text = "@mbid";
                parameters[0].Value = mbid;

                return  TrasenFrame.Forms.FrmMdiMain.Database.GetDataTable("SP_JC_CFMB_SELECT", parameters, 30);
                
            }
            catch (System.Exception err)
            {
                throw new System.Exception(err.ToString());
            }
        }
        #endregion

        #region 其他方法
        private void Language_Off(object sender, System.EventArgs e)
        {
            Control control = (Control)sender;

            control.ImeMode = ImeMode.Close;
            Fun.SetInputLanguageOff();
        }

        private void Language_On(object sender, System.EventArgs e)
        {
            Control control = (Control)sender;
            control.ImeMode = ImeMode.On;
            Fun.SetInputLanguageOff();
        }

        #endregion

        #region 左边列表相关事件
        //常用药品双击事件
        private void listView_cyyp_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                DataTable tb = (DataTable)dataGridView1.DataSource;
                //butnew_Click(sender, e);
                //if (Dqcf.jzid == 0) { MessageBox.Show("请选择相应的病人", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                int nrow = cell.nrow;


                string kslx="";
                if (rdomzyf.Checked==true)
                    kslx=rdomzyf.Text;
                else 
                    kslx=rdozyyf.Text;
               
                ListViewItem item = (ListViewItem)listView_cyyp.SelectedItems[0];
                string ggid = item.SubItems["ggid"].Text;
                string cjid = item.SubItems["cjid"].Text;
                string cjwhere = "项目ID=" + cjid + " AND 项目来源=1 ";

                string DeptName = "";
                if (Dqcf.zxksid > 0)
                {
                    cjwhere = cjwhere + "  and zxksid=" + Dqcf.zxksid + "";
                    DeptName = Fun.SeekDeptName(Dqcf.zxksid);
                }
                else
                {
                    cjwhere = cjwhere + " and kslx2='" + kslx + "'";
                    DeptName = kslx;
                }

                DataRow[] rowcj = PubDset.Tables["ITEM"].Select(cjwhere);
                

                if (rowcj.Length > 0)
                {
                    Addrow(rowcj[0], ref nrow);
                }
                else
                {
                    string ggwhere = "ggid=" + ggid + " and 项目ID<>" + cjid + " AND 项目来源=1  ";
                    if (Dqcf.zxksid > 0)
                        ggwhere = ggwhere + "  and zxksid=" + Dqcf.zxksid + "";
                    else
                        ggwhere = ggwhere + " and kslx2='" + kslx + "'";
                    DataRow[] rowgg = PubDset.Tables["ITEM"].Select(ggwhere);
                    if (rowgg.Length > 0)
                    {
                        Addrow(rowgg[0], ref nrow);
                    }
                    else
                    {
                        MessageBox.Show("在"+DeptName+"中没有找到药品","",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        return;
                    }
                }


                if (nrow == tb.Rows.Count - 1 && tb.Rows[nrow]["项目id"].ToString() != "")
                {
                    DataRow row = tb.NewRow();
                    row["修改"] = true;
                    tb.Rows.Add(row);
                    dataGridView1.DataSource = tb;
                    nrow = nrow + 1;
                }

                dataGridView1.CurrentCell = dataGridView1["剂量", nrow];

                //ModifCfje(tb, tb.Rows[nrow]["hjid"].ToString());
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //常用项目双击事件
        private void listView_cyxm_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                DataTable tb = (DataTable)dataGridView1.DataSource;
                //butnew_Click(sender, e);
                //if (Dqcf.jzid == 0) { MessageBox.Show("请选择相应的病人", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                int nrow = cell.nrow;

                DataRow[] rowx = tb.Select("hjid=0 and 执行科室ID>0 ");
                int zxksid = 0;
                if (rowx.Length>0)
                    zxksid = Convert.ToInt32(rowx[0]["执行科室ID"]);

                ListViewItem item = (ListViewItem)listView_cyxm.SelectedItems[0];
                string orderid = item.SubItems["order_id"].Text;
                string where = "yzID=" + orderid + " AND 项目来源=2 ";

                DataRow[] rows = PubDset.Tables["ITEM"].Select(where);


                if (rows.Length > 0)
                {
                    if (zxksid != Convert.ToInt32(rows[0]["zxksid"]))
                    {
                        MessageBox.Show("不同的执行科室,不能开在同一个处方上", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Addrow(rows[0], ref nrow);
                }
                else
                {
                        MessageBox.Show("没有找到该医嘱项目,可能已停用", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                }


                if (nrow == tb.Rows.Count - 1 && tb.Rows[nrow]["项目id"].ToString() != "")
                {
                    DataRow row = tb.NewRow();
                    row["修改"] = true;
                    tb.Rows.Add(row);
                    dataGridView1.DataSource = tb;
                    nrow = nrow + 1;
                }

                dataGridView1.CurrentCell = dataGridView1["剂量", nrow];

            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //模板双击事件
        private void listView_mb_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                ListViewItem item = (ListViewItem)listView_mb.SelectedItems[0];
                butnew_Click(sender, e);
                //DataTable tb = (DataTable)dataGridView1.DataSource;
                
                string mbid = item.SubItems["mbid"].Text;
                Dqcf.mbid = Convert.ToInt64(mbid);
                DataTable tb= Read_Mb(Convert.ToInt64(mbid));
                AddPresc(tb);
                if (Convert.ToInt64(mbid) > 0) butsgmc.Enabled = true;

                string ssql = "select * from jc_cfmb where mbid=" + mbid + "";
                DataTable tbmb = InstanceForm.BDatabase.GetDataTable(ssql);
                if (tbmb.Rows.Count > 0)
                {
                    //panel1.Visible = true;
                    lblmbmc.Text = tbmb.Rows[0]["mbmc"].ToString();
                    lbldjsj.Text = tbmb.Rows[0]["djsj"].ToString();
                    lbldjy.Text = Fun.SeekEmpName(Convert.ToInt32(tbmb.Rows[0]["djy"]));
                }
            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message,"",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
            
        }
        #endregion

        private void butsgmc_Click(object sender, EventArgs e)
        {
            try
            {
                string _mbmc = "";
                string _newmbmc = "";
                if (Dqcf.mbid >= 0)
                {
                    string ssql = "select * from jc_cfmb where mbid=" + Dqcf.mbid + "";
                    DataTable tbmb = InstanceForm.BDatabase.GetDataTable(ssql);
                    if (tbmb.Rows.Count > 0) _mbmc = tbmb.Rows[0]["mbmc"].ToString().Trim();
                }

                DlgInputBox Inputbox = new DlgInputBox(_mbmc, "请输入新模板名称", "保存模板");
                Inputbox.NumCtrl = false;
                Inputbox.ShowDialog();
                if (!DlgInputBox.DlgResult) return;
                _newmbmc = DlgInputBox.DlgValue.ToString();
                if (_newmbmc.Trim() == "") { MessageBox.Show("模板名称不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

                jc_mb.UpdateMbmc(Dqcf.mbid, _newmbmc);

                MessageBox.Show("修改成功");
                Select_Mb();

            }
            catch (System.Exception err)
            {
                MessageBox.Show(err.Message, "修改模板名", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        



    }
}