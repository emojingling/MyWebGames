using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using MyHelpers;

namespace DrawGuess
{
    public class GuessDbHelper
    {
        /// <summary>头像图片目录</summary>
        public const string AvatarPath = "~/Images/Avatar/";

        public GuessDbHelper(string connStr = null)
        {
            if (string.IsNullOrEmpty(connStr))
                connStr = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString;
            MySqlHelper.ConnStr = connStr;
        }

        #region Login page

        /// <summary>判断用户账户是否已存在</summary>
        /// <param name="account">用户账户名</param>
        /// <returns></returns>
        public bool ExistUser(string account)
        {
            if (string.IsNullOrEmpty(account)) return false;

            string sql =string.Format(
                @"if exists (select * from user_table where [USER_ACCOUNT] = '{0}') select '1' else select '0'",
                account);
            var obj = SqlServerHelper.GetSingle(sql);
            int res;
            if (obj == null || obj is DBNull || !int.TryParse(obj.ToString(), out res)) return false;
            return res == 1;
        }

        /// <summary>添加用户</summary>
        /// <param name="account"></param>
        /// <param name="name"></param>
        /// <param name="pass"></param>
        /// <param name="sex"></param>
        /// <param name="picName"></param>
        /// <param name="ipAddress"></param>
        /// <param name="browser"></param>
        /// <returns></returns>
        public bool AddUser(string account, string name, string pass, int sex, string picName, string ipAddress = null,
            string browser = null)
        {
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pass) ||
                string.IsNullOrEmpty(picName))
                return false;
            if (account.Length > 255) return false;
            if (name.Length > 100) return false;
            if (pass.Length > 50) return false;
            if (ExistUser(account)) return false;

            if (sex > 1) sex = 1;
            if (sex < 0) sex = 0;

            var sqlBuilder = new StringBuilder("insert into user_table (USER_NAME,USER_ACCOUNT,USER_PASS,");
            sqlBuilder.Append("USER_SEX,PIC_PATH,REG_TYPE,REG_TIME,LAST_IP,LAST_BROWSER) ");
            sqlBuilder.Append("values (@USER_NAME,@USER_ACCOUNT,@USER_PASS,@USER_SEX,PIC_PATH,@REG_TYPE,");
            sqlBuilder.Append("@REG_TIME,@LAST_IP,@LAST_BROWSER)");
            SqlParameter[] pars =
            {
                new SqlParameter("USER_NAME", SqlDbType.VarChar),
                new SqlParameter("USER_ACCOUNT", SqlDbType.VarChar),
                new SqlParameter("USER_PASS", SqlDbType.VarChar),
                new SqlParameter("USER_SEX", SqlDbType.Int),
                new SqlParameter("PIC_PATH", SqlDbType.VarChar),
                new SqlParameter("REG_TYPE", SqlDbType.VarChar),
                new SqlParameter("REG_TIME", SqlDbType.Timestamp),
                new SqlParameter("LAST_IP", SqlDbType.VarChar),
                new SqlParameter("LAST_BROWSER", SqlDbType.VarChar)
            };
            pars[0].Value = name;
            pars[1].Value = account;
            pars[2].Value = pass;
            pars[3].Value = sex;
            pars[4].Value = picName;
            pars[5].Value = "Unknown";
            pars[6].Value = DateTime.Now.ToFileTime();
            pars[7].Value = string.IsNullOrEmpty(ipAddress) ? string.Empty : ipAddress;
            pars[8].Value = string.IsNullOrEmpty(browser) ? string.Empty : browser;

            return SqlServerHelper.ExecuteSql(sqlBuilder.ToString(), pars) > 0;
        }




        #endregion





    }
}