using Microsoft.Data.SqlClient;
using System.Data;
using ADDPerformance.Data;

namespace ADDPerformance.Models
{
    public class ReadFromTable
    {
        private readonly DBContext _context;

        public ReadFromTable(DBContext context)
        {
            _context = context;
        }
        public DataTable ReadFrometfpr(String cmd)
        {
            string connectionString = "Server=svhqosd01;initial catalog= etfpr;Trusted_Connection=False;Encrypt=False;user id=etfpr;password=abcd1234;MultipleActiveResultSets=True;App=EntityFramework";

            SqlConnection con = new SqlConnection();
            try
            {
                con.ConnectionString = connectionString;
                con.Open();
                DataTable tbl = new DataTable();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd, con);
                adapter.Fill(tbl);
                tbl.TableName = "tblStatus";
                return tbl;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
        }
    }
}
