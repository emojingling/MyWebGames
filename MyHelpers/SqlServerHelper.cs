using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace MyHelpers
{
    /// <summary>
    /// SQLServer帮助类
    /// </summary>
    /// <remarks>chenz, 2017.03.27</remarks>
    public class SqlServerHelper
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public static string ConnStr;

        static SqlServerHelper()
        {
            try
            {
                ConnStr = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString;
            }
            catch (Exception e)
            {
                //LogHelper.WriteLog(nameof(SqlServerHelper), e);
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
            string strsql = "select max(" + fieldName + ")+1 from " + tableName;
            object obj = GetSingle(strsql);

            if (obj == null) return 1;
            return int.Parse(obj.ToString());
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="cmdParms">参数数组</param>
        /// <returns></returns>
        public static bool Exists(string sql, params SqlParameter[] cmdParms)
        {
            object obj = GetSingle(sql, cmdParms);
            int cmdresult;
            if (obj == null || (Equals(obj, DBNull.Value)))
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
        /// 判断数据表是否存在
        /// </summary>
        /// <param name="tableName">数据表名</param>
        /// <returns></returns>
        public static bool IsTableExist(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return false;

            string sql = string.Format("if object_id('{0}') is not null select 1 else select 0", tableName);
            int result;
            int.TryParse(GetSingle(sql).ToString(), out result);
            return result == 1;
        }

        #endregion

        #region  执行简单SQL语句

        /// <summary>执行SQL语句，返回影响的记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string sql)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    try
                    {
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        return rows;
                    }
                    catch (SqlException e)
                    {
                        LogHelper.WriteLog(nameof(ExecuteSql), e);
                        return -1;
                    }
                    finally
                    {
                        conn.Close();
                    }
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

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand { Connection = conn };
                SqlTransaction tx = conn.BeginTransaction();
                cmd.Transaction = tx;
                try
                {
                    int count = sqls.Count;
                    for (int n = 0; n < count; n++)
                    {
                        string strsql = sqls[n];
                        if (strsql.Trim().Length > 1)
                        {
                            cmd.CommandText = strsql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                    return true;
                }
                catch (SqlException e)
                {
                    tx.Rollback();
                    LogHelper.WriteLog(nameof(ExecuteSqlTran), e);
                    return false;
                }
                finally
                {
                    cmd.Dispose();
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
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlParameter myParameter = new SqlParameter("@content", SqlDbType.NText) { Value = content };
                cmd.Parameters.Add(myParameter);
                try
                {
                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (SqlException e)
                {
                    LogHelper.WriteLog(nameof(ExecuteSql), e);
                    return -1;
                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
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
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlParameter myParameter = new SqlParameter("@fs", SqlDbType.Image) { Value = fs };
                cmd.Parameters.Add(myParameter);
                try
                {
                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (SqlException e)
                {
                    LogHelper.WriteLog(nameof(ExecuteSqlInsertImg), e);
                    return -1;
                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">计算查询结果语句</param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sql)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    try
                    {
                        conn.Open();
                        object obj = cmd.ExecuteScalar();
                        if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
                        {
                            return null;
                        }
                        else
                        {
                            return obj;
                        }
                    }
                    catch (SqlException e)
                    {
                        LogHelper.WriteLog(nameof(GetSingle), e);
                        return -1;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行查询语句，返回SqlDataReader
        /// </summary>
        /// <param name="conn">SQLServer连接</param>
        /// <param name="sql">查询语句</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader ExecuteReader(SqlConnection conn, string sql)
        {
            SqlCommand cmd = new SqlCommand(sql, conn);
            try
            {
                conn.Open();
                SqlDataReader myReader = cmd.ExecuteReader();
                return myReader;
            }
            catch (SqlException e)
            {
                LogHelper.WriteLog(nameof(ExecuteReader), e);
                return null;
            }
            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <returns>DataSet</returns>
        public static DataSet Query(string sql)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                DataSet ds = new DataSet();
                try
                {
                    conn.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                    adapter.Fill(ds);
                    adapter.Dispose();
                }
                catch (SqlException e)
                {
                    LogHelper.WriteLog(nameof(Query), e);
                    return null;
                }
                finally
                {
                    conn.Close();
                }
                return ds;
            }
        }

        /// <summary>
        /// 得到DataTable
        /// </summary>
        /// <param name="connStr">连接字符串</param>
        /// <param name="sql">SELECT SQL语句</param>
        /// <returns></returns>
        /// <remarks>未得到时返回null</remarks>
        public static DataTable GetTable(string connStr, string sql)
        {
            DataSet ds = new DataSet();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    SqlDataAdapter adapter = new SqlDataAdapter { SelectCommand = cmd };
                    adapter.Fill(ds);
                }
                catch (SqlException e)
                {
                    LogHelper.WriteLog(nameof(GetTable), e);
                    return null;
                }
            }
            if (ds.Tables.Count == 0) return null;
            if (ds.Tables.Count != 1) throw new Exception("得到的DataTable数量不为1！");
            return ds.Tables[0];
        }

        /// <summary>
        /// 得到DataTable
        /// </summary>
        /// <param name="sql">SELECT SQL语句</param>
        /// <returns></returns>
        /// <remarks>未得到时返回null</remarks>
        public static DataTable GetTable(string sql)
        {
            return GetTable(ConnStr, sql);
        }

        #endregion

        #region 执行带参数的SQL语句

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="cmdParms"></param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string sql, params SqlParameter[] cmdParms)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, conn, null, sql, cmdParms);
                        int rows = cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                        return rows;
                    }
                    catch (SqlException e)
                    {
                        LogHelper.WriteLog(nameof(ExecuteSql), e);
                        return -1;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }


        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="sqlHash">SQL语句的哈希表（key为sql语句，value是该语句的SqlParameter[]）</param>
        /// <remarks>哈希表中的key为sql语句，value是该语句的SqlParameter[]</remarks>
        public static void ExecuteSqlTran(Hashtable sqlHash)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    SqlCommand cmd = new SqlCommand();
                    try
                    {
                        //循环
                        foreach (DictionaryEntry de in sqlHash)
                        {
                            string cmdText = de.Key.ToString();
                            SqlParameter[] cmdParms = (SqlParameter[])de.Value;
                            PrepareCommand(cmd, conn, trans, cmdText, cmdParms);
                            // ReSharper disable once UnusedVariable
                            int val = cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();

                            trans.Commit();
                        }
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">计算查询结果语句</param>
        /// <param name="cmdParms"></param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sql, params SqlParameter[] cmdParms)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, conn, null, sql, cmdParms);
                        object obj = cmd.ExecuteScalar();
                        cmd.Parameters.Clear();
                        if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
                        {
                            return null;
                        }
                        return obj;
                    }
                    catch (SqlException e)
                    {
                        LogHelper.WriteLog(nameof(GetSingle), e);
                        return null;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行查询语句，返回SqlDataReader
        /// </summary>
        /// <param name="conn">连接字符串</param>
        /// <param name="sql">查询语句</param>
        /// <param name="cmdParms"></param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader ExecuteReader(SqlConnection conn, string sql, params SqlParameter[] cmdParms)
        {
            SqlCommand cmd = new SqlCommand();
            try
            {
                PrepareCommand(cmd, conn, null, sql, cmdParms);
                SqlDataReader reader = cmd.ExecuteReader();
                cmd.Parameters.Clear();
                return reader;
            }
            catch (SqlException e)
            {
                LogHelper.WriteLog(nameof(ExecuteReader), e);
                return null;
            }

        }

        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="cmdParms"></param>
        /// <returns>DataSet</returns>
        public static DataSet Query(string sql, params SqlParameter[] cmdParms)
        {
            using (SqlConnection connection = new SqlConnection(ConnStr))
            {
                SqlCommand cmd = new SqlCommand();
                PrepareCommand(cmd, connection, null, sql, cmdParms);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    try
                    {
                        da.Fill(ds, "ds");
                        cmd.Parameters.Clear();
                    }
                    catch (SqlException e)
                    {
                        LogHelper.WriteLog(nameof(Query), e);
                        return null;
                    }
                    return ds;
                }
            }
        }

        /// <summary>
        /// 为执行命令准备参数
        /// </summary>
        /// <param name="cmd">SqlCommand 命令</param>
        /// <param name="conn">已经存在的数据库连接</param>
        /// <param name="trans">数据库事物处理</param>
        /// <param name="cmdType">SqlCommand命令类型 (存储过程， T-SQL语句， 等等。)</param>
        /// <param name="cmdText">Command text，T-SQL语句 例如 Select * from Products</param>
        /// <param name="cmdParms">返回带参数的命令</param>
        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms)
        {
            //判断数据库连接状态
            if (conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            //判断是否需要事物处理
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = cmdType;
            if (cmdParms != null)
            {
                foreach (SqlParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }

        /// <summary>
        /// 为执行命令准备参数
        /// </summary>
        /// /// <param name="cmd">SqlCommand 命令</param>
        /// <param name="conn">已经存在的数据库连接</param>
        /// <param name="trans">数据库事物处理</param>
        /// <param name="cmdText">Command text，T-SQL语句 例如 Select * from Products</param>
        /// <param name="cmdParms">返回带参数的命令</param>
        /// <remarks>CommandType为CommandType.Text</remarks>
        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, string cmdText, SqlParameter[] cmdParms)
        {
            PrepareCommand(cmd, conn, trans, CommandType.Text, cmdText, cmdParms);
        }

        /// <summary>
        /// 执行一条返回结果集的SqlCommand，通过一个已经存在的数据库连接
        /// 使用参数数组提供参数
        /// </summary>
        /// <param name="connStr">一个现有的数据库连接</param>
        /// <param name="cmdTye">SqlCommand命令类型</param>
        /// <param name="cmdText">存储过程的名字或者 T-SQL 语句</param>
        /// <param name="commandParameters">以数组形式提供SqlCommand命令中用到的参数列表</param>
        /// <returns>返回一个表集合(DataTableCollection)表示查询得到的数据集</returns>
        public static DataTableCollection GetTables(string connStr, CommandType cmdTye, string cmdText, SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            DataSet ds = new DataSet();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                PrepareCommand(cmd, conn, null, cmdTye, cmdText, commandParameters);
                SqlDataAdapter adapter = new SqlDataAdapter { SelectCommand = cmd };
                adapter.Fill(ds);
            }
            DataTableCollection tables = ds.Tables;
            return tables;
        }

        /// <summary>
        /// 执行一条返回结果集的SqlCommand，通过一个已经存在的数据库连接
        /// 使用参数数组提供参数
        /// </summary>
        /// <param name="cmdTye">SqlCommand命令类型</param>
        /// <param name="cmdText">存储过程的名字或者 T-SQL 语句</param>
        /// <param name="commandParameters">以数组形式提供SqlCommand命令中用到的参数列表</param>
        /// <returns>返回一个表集合(DataTableCollection)表示查询得到的数据集</returns>
        public static DataTableCollection GetTables(CommandType cmdTye, string cmdText, SqlParameter[] commandParameters)
        {
            return GetTables(ConnStr, cmdTye, cmdText, commandParameters);
        }

        /// <summary>
        /// 获得数据表集合，Sql语句专用
        /// </summary>
        /// <param name="cmdText"> T-SQL 语句</param>
        /// <param name="commandParameters">以数组形式提供SqlCommand命令中用到的参数列表</param>
        /// <returns>返回一个表集合(DataTableCollection)表示查询得到的数据集</returns>
        public static DataTableCollection GetTablesText(string cmdText, SqlParameter[] commandParameters)
        {
            return GetTables(CommandType.Text, cmdText, commandParameters);
        }

        /// <summary>
        /// 返回DataTable
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="type">语句类型（字符串或者存储过程）</param>
        /// <param name="pars">参数列表</param>
        /// <returns></returns>
        public static DataTable GetDataTable(string sql, CommandType type, params SqlParameter[] pars)
        {
            using (SqlConnection con = new SqlConnection(ConnStr))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
                {
                    adapter.SelectCommand.CommandType = type;
                    if (pars != null)
                    {
                        adapter.SelectCommand.Parameters.AddRange(pars);//Parameter的作用在于防止注入式攻击
                    }
                    DataTable dt = new DataTable();
                    try
                    {
                        adapter.Fill(dt);

                    }
                    catch (SqlException e)
                    {
                        LogHelper.WriteLog(nameof(GetDataTable), e);
                        throw new Exception(e.Message);
                    }
                    return dt;
                }
            }
        }

        #endregion

        #region 存储过程操作

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader RunProcedure(string storedProcName, IDataParameter[] parameters)
        {
            SqlConnection connection = new SqlConnection(ConnStr);
            connection.Open();
            SqlCommand command = BuildQueryCommand(connection, storedProcName, parameters);
            command.CommandType = CommandType.StoredProcedure;
            SqlDataReader reader = command.ExecuteReader();
            return reader;
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <param name="tableName">DataSet结果中的表名</param>
        /// <returns>DataSet</returns>
        public static DataSet RunProcedure(string storedProcName, IDataParameter[] parameters, string tableName)
        {
            using (SqlConnection connection = new SqlConnection(ConnStr))
            {
                DataSet dataSet = new DataSet();
                connection.Open();
                SqlDataAdapter adapter = new SqlDataAdapter
                {
                    SelectCommand = BuildQueryCommand(connection, storedProcName, parameters)
                };
                adapter.Fill(dataSet, tableName);
                connection.Close();
                return dataSet;
            }
        }

        /// <summary>
        /// 构建 SqlCommand 对象(用来返回一个结果集，而不是一个整数值)
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <returns>SqlCommand</returns>
        private static SqlCommand BuildQueryCommand(SqlConnection connection, string storedProcName, IDataParameter[] parameters)
        {
            SqlCommand cmd = new SqlCommand(storedProcName, connection) { CommandType = CommandType.StoredProcedure };
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (SqlParameter parameter in parameters)
            {
                cmd.Parameters.Add(parameter);
            }
            return cmd;
        }

        /// <summary>
        /// 执行存储过程，返回影响的行数  
        /// </summary>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <param name="rowsAffected">影响的行数</param>
        /// <returns></returns>
        public static int RunProcedure(string storedProcName, IDataParameter[] parameters, out int rowsAffected)
        {
            int result = -1;
            rowsAffected = 0;
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand command = BuildIntCommand(conn, storedProcName, parameters);
                    rowsAffected = command.ExecuteNonQuery();
                    result = (int)command.Parameters["ReturnValue"].Value;
                }
                catch (SqlException e)
                {
                    LogHelper.WriteLog(nameof(Query), e);
                }
                finally
                {
                    conn.Close();
                }
                return result;
            }
        }

        /// <summary>
        /// 创建 SqlCommand 对象实例(用来返回一个整数值) 
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <returns>SqlCommand 对象实例</returns>
        private static SqlCommand BuildIntCommand(SqlConnection conn, string storedProcName, IDataParameter[] parameters)
        {
            SqlCommand command = BuildQueryCommand(conn, storedProcName, parameters);
            command.Parameters.Add(new SqlParameter("ReturnValue",
             SqlDbType.Int, 4, ParameterDirection.ReturnValue,
             false, 0, 0, String.Empty, DataRowVersion.Default, null));
            return command;
        }

        /// <summary>
        /// 获得数据表集合，存储过程专用
        /// </summary>
        /// <param name="cmdText">存储过程的名字或者 T-SQL 语句</param>
        /// <param name="commandParameters">以数组形式提供SqlCommand命令中用到的参数列表</param>
        /// <returns>返回一个表集合(DataTableCollection)表示查询得到的数据集</returns>
        public static DataTableCollection GetTableProducts(string cmdText, SqlParameter[] commandParameters)
        {
            return GetTables(CommandType.StoredProcedure, cmdText, commandParameters);
        }

        #endregion
    }
}
