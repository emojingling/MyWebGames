using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace MyHelpers
{
    /// <summary>
    /// MySQL帮助类
    /// </summary>
    /// <remarks>chenz, 2017.03.27</remarks>
    public abstract class MySqlHelper
    {
        /// <summary>
        /// 数据库连接字符串(web.config来配置)
        /// </summary>
        protected static string ConnStr;

        static MySqlHelper()
        {
            try
            {
                ConnStr = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString;
            }
            catch (Exception e)
            {
                LogHelper.WriteLog(nameof(SqlServerHelper), e);
            }
        }

        #region 公用方法

        /// <summary>
        /// 得到最大值
        /// </summary>
        /// <param name="fieldName">列名</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public static int GetMaxID(string fieldName, string tableName)
        {
            var strsql = "select max(" + fieldName + ")+1 from " + tableName;
            var obj = GetSingle(strsql);
            if (obj == null) return 1;

            return int.Parse(obj.ToString());
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public static bool Exists(string sql)
        {
            var obj = GetSingle(sql);
            int cmdresult;
            if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
            {
                cmdresult = 0;
            }
            else
            {
                cmdresult = int.Parse(obj.ToString());
            }
            return cmdresult != 0;
        }

        /// <summary>
        /// 是否存在（基于MySqlParameter）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="cmdParms">参数数组</param>
        /// <returns></returns>
        public static bool Exists(string sql, params MySqlParameter[] cmdParms)
        {
            var obj = GetSingle(sql, cmdParms);
            int cmdresult;
            if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
            {
                cmdresult = 0;
            }
            else
            {
                cmdresult = int.Parse(obj.ToString());
            }
            return cmdresult != 0;
        }

        #endregion

        #region  执行简单SQL语句  

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string sql)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand(sql, connection);
                try
                {
                    connection.Open();
                    var rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (MySqlException e)
                {
                    LogHelper.WriteLog(nameof(ExecuteSql), e);
                    return -1;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="timeout">过期时间</param>
        /// <returns></returns>
        public static int ExecuteSql(string sql, int timeout)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand(sql, connection);
                try
                {
                    connection.Open();
                    cmd.CommandTimeout = timeout;
                    var rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (MySqlException e)
                {
                    LogHelper.WriteLog(nameof(ExecuteSql), e);
                    return -1;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="sqls">多条SQL语句</param>
        public static bool ExecuteSqlTran(IList<string> sqls)
        {
            if (sqls == null || sqls.Count == 0) return true;

            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                var cmd = new MySqlCommand {Connection = conn};
                var tx = conn.BeginTransaction();
                cmd.Transaction = tx;
                try
                {
                    var count = sqls.Count;
                    for (var n = 0; n < count; n++)
                    {
                        var strsql = sqls[n];
                        if (strsql.Trim().Length > 1)
                        {
                            cmd.CommandText = strsql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                    return true;
                }
                catch (MySqlException e)
                {
                    tx.Rollback();
                    LogHelper.WriteLog(nameof(ExecuteSqlTran), e);
                    return false;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 执行带一个存储过程参数的的SQL语句。
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="content">参数内容,比如一个字段是格式复杂的文章，有特殊符号，可以通过这个方式添加</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string sql, string content)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand(sql, connection);
                var myParameter = new MySqlParameter("@content", SqlDbType.NText) {Value = content};
                cmd.Parameters.Add(myParameter);
                try
                {
                    connection.Open();
                    var rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (MySqlException e)
                {
                    LogHelper.WriteLog(nameof(ExecuteSql), e);
                    return -1;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 执行带一个存储过程参数的的SQL语句。
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="content">参数内容,比如一个字段是格式复杂的文章，有特殊符号，可以通过这个方式添加</param>
        /// <returns>影响的记录数</returns>
        public static object ExecuteSqlGet(string sql, string content)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand(sql, connection);
                var myParameter = new MySqlParameter("@content", SqlDbType.NText) {Value = content};
                cmd.Parameters.Add(myParameter);
                try
                {
                    connection.Open();
                    var obj = cmd.ExecuteScalar();
                    if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
                    {
                        return null;
                    }
                    else
                    {
                        return obj;
                    }
                }
                catch (MySqlException e)
                {
                    LogHelper.WriteLog(nameof(ExecuteSqlGet), e);
                    return -1;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 向数据库里插入图像格式的字段(和上面情况类似的另一种实例)
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="fs">图像字节,数据库的字段类型为image的情况</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSqlInsertImg(string sql, byte[] fs)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand(sql, connection);
                var myParameter = new MySqlParameter("@fs", SqlDbType.Image) {Value = fs};
                cmd.Parameters.Add(myParameter);
                try
                {
                    connection.Open();
                    var rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (MySqlException e)
                {
                    LogHelper.WriteLog(nameof(ExecuteSqlInsertImg), e);
                    return -1;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）
        /// </summary>
        /// <param name="sql">计算查询结果语句</param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sql)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand(sql, connection);
                try
                {
                    connection.Open();
                    var obj = cmd.ExecuteScalar();
                    if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
                    {
                        return null;
                    }
                    else
                    {
                        return obj;
                    }
                }
                catch (MySqlException e)
                {
                    LogHelper.WriteLog(nameof(GetSingle), e);
                    return null;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）
        /// </summary>
        /// <param name="sql">计算查询结果语句</param>
        /// <param name="outTime">过期时间</param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sql, int outTime)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand(sql, connection);
                try
                {
                    connection.Open();
                    cmd.CommandTimeout = outTime;
                    var obj = cmd.ExecuteScalar();
                    if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
                    {
                        return null;
                    }
                    else
                    {
                        return obj;
                    }
                }
                catch (MySqlException e)
                {
                    LogHelper.WriteLog(nameof(GetSingle), e);
                    return null;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 执行查询语句，返回MySqlDataReader ( 注意：调用该方法后，一定要对MySqlDataReader进行Close )
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <returns>MySqlDataReader</returns>
        public static MySqlDataReader ExecuteReader(string sql)
        {
            var connection = new MySqlConnection(ConnStr);
            var cmd = new MySqlCommand(sql, connection);
            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                return reader;
            }
            catch (MySqlException e)
            {
                LogHelper.WriteLog(nameof(ExecuteReader), e);
                throw;
            }
        }

        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <returns>DataSet</returns>
        public static DataSet Query(string sql)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var ds = new DataSet();
                try
                {
                    connection.Open();
                    var adapter = new MySqlDataAdapter(sql, connection);
                    adapter.Fill(ds, "ds");
                }
                catch (MySqlException ex)
                {
                    LogHelper.WriteLog(nameof(Query), ex);
                    return null;
                }
                finally
                {
                    connection.Close();
                }
                return ds;
            }
        }

        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="outTime">过期时间</param>
        /// <returns>DataSet</returns>
        public static DataSet Query(string sql, int outTime)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var ds = new DataSet();
                try
                {
                    connection.Open();
                    var adapter = new MySqlDataAdapter(sql, connection)
                    {
                        SelectCommand = {CommandTimeout = outTime}
                    };
                    adapter.Fill(ds, "ds");
                }
                catch (MySqlException ex)
                {
                    LogHelper.WriteLog(nameof(Query), ex);
                    return null;
                }
                finally
                {
                    connection.Close();
                }
                return ds;
            }
        }

        /// <summary>
        /// 执行查询语句，返回DataTable
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <returns>DataSet</returns>
        public static DataTable GetTable(string sql)
        {
            var ds = Query(sql);
            if (ds == null || ds.Tables.Count == 0) return null;

            return ds.Tables[0];
        }

        /// <summary>
        /// 执行查询语句，返回DataTable
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="outTime">过期时间</param>
        /// <returns>DataSet</returns>
        public static DataTable GetTable(string sql, int outTime)
        {
            var ds = Query(sql, outTime);
            if (ds == null || ds.Tables.Count == 0) return null;

            return ds.Tables[0];
        }

        #endregion

        #region 执行带参数的SQL语句

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="cmdParms">参数数组</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string sql, params MySqlParameter[] cmdParms)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand();
                try
                {
                    PrepareCommand(cmd, connection, null, sql, cmdParms);
                    var rows = cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    return rows;
                }
                catch (MySqlException ex)
                {
                    LogHelper.WriteLog(nameof(ExecuteSql), ex);
                    return -1;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }


        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="sqls">SQL语句的哈希表（key为sql语句，value是该语句的MySqlParameter[]）</param>
        public static bool ExecuteSqlTran(Hashtable sqls)
        {
            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    var cmd = new MySqlCommand();
                    try
                    {
                        foreach (DictionaryEntry de in sqls)
                        {
                            var cmdText = de.Key.ToString();
                            var cmdParms = (MySqlParameter[]) de.Value;
                            PrepareCommand(cmd, conn, trans, cmdText, cmdParms);
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                        }
                        trans.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        LogHelper.WriteLog(nameof(ExecuteSqlTran), ex);
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="sqls">SQL语句的哈希表（key为sql语句，value是该语句的MySqlParameter[]）</param>
        public static bool ExecuteSqlTranWithIndentity(Hashtable sqls)
        {
            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    var cmd = new MySqlCommand();
                    try
                    {
                        var indentity = 0;
                        foreach (DictionaryEntry de in sqls)
                        {
                            var cmdText = de.Key.ToString();
                            var cmdParms = (MySqlParameter[]) de.Value;
                            foreach (var q in cmdParms)
                            {
                                if (q.Direction == ParameterDirection.InputOutput)
                                {
                                    q.Value = indentity;
                                }
                            }
                            PrepareCommand(cmd, conn, trans, cmdText, cmdParms);
                            cmd.ExecuteNonQuery();
                            foreach (var q in cmdParms)
                            {
                                if (q.Direction == ParameterDirection.Output)
                                {
                                    indentity = Convert.ToInt32(q.Value);
                                }
                            }
                            cmd.Parameters.Clear();
                        }
                        trans.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        LogHelper.WriteLog(nameof(ExecuteSqlTranWithIndentity), ex);
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">计算查询结果语句</param>
        /// <param name="cmdParms">参数数组</param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sql, params MySqlParameter[] cmdParms)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                using (var cmd = new MySqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, connection, null, sql, cmdParms);
                        var obj = cmd.ExecuteScalar();
                        cmd.Parameters.Clear();
                        if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
                        {
                            return null;
                        }
                        return obj;
                    }
                    catch (MySqlException ex)
                    {
                        LogHelper.WriteLog(nameof(GetSingle), ex);
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// 执行查询语句，返回MySqlDataReader ( 注意：调用该方法后，一定要对MySqlDataReader进行Close )
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="cmdParms">参数数组</param>
        /// <returns>MySqlDataReader</returns>
        public static MySqlDataReader ExecuteReader(string sql, params MySqlParameter[] cmdParms)
        {
            var connection = new MySqlConnection(ConnStr);
            var cmd = new MySqlCommand();
            try
            {
                PrepareCommand(cmd, connection, null, sql, cmdParms);
                var myReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return myReader;
            }
            catch (MySqlException ex)
            {
                LogHelper.WriteLog(nameof(ExecuteReader), ex);
                return null;
            }
        }

        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="cmdParms">参数数组</param>
        /// <returns>DataSet</returns>
        public static DataSet Query(string sql, params MySqlParameter[] cmdParms)
        {
            using (var connection = new MySqlConnection(ConnStr))
            {
                var cmd = new MySqlCommand();
                PrepareCommand(cmd, connection, null, sql, cmdParms);
                using (var da = new MySqlDataAdapter(cmd))
                {
                    var ds = new DataSet();
                    try
                    {
                        da.Fill(ds, "ds");
                        cmd.Parameters.Clear();
                    }
                    catch (MySqlException ex)
                    {
                        LogHelper.WriteLog(nameof(Query), ex);
                        return null;
                    }
                    return ds;
                }
            }
        }

        private static void PrepareCommand(MySqlCommand cmd, MySqlConnection conn, MySqlTransaction trans,
            string cmdText, MySqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = CommandType.Text; //cmdType;  
            if (cmdParms != null)
            {
                foreach (var parameter in cmdParms)
                {
                    if ((parameter.Direction == ParameterDirection.InputOutput ||
                         parameter.Direction == ParameterDirection.Input) &&
                        (parameter.Value == null))
                    {
                        parameter.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(parameter);
                }
            }
        }

        #endregion
    }
}