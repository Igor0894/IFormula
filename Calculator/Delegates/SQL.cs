using System.Data.SqlClient;

namespace Interpreter.Delegates
{
    public class SQL
    {
        public string Address { get; set; } = "local";
        public string Database { get; set; } = "ISP";
        public bool SSPI { get; set; } = true;
        public string User { get; set; } = "sa";
        public string Password { get; set; } = "";
        private string ConnectionString
        {
            get => SSPI ? $"Data Source={Address};Initial Catalog={Database};Integrated Security=True;" :
                $"Data Source={Address};Initial Catalog={Database};User Id={User};Password={Password}";
        }
        public void UpdateSQLValue(Guid attributeId, string? value, DateTime timeStamp)
        {
            string query = @"IF EXISTS(SELECT * FROM TimedValue WHERE ElementAttributeId = @AttributeId AND Time = @TimeStamp)
                        UPDATE TimedValue
                        SET Value = @Value
                        WHERE ElementAttributeId = @AttributeId AND Time = @TimeStamp
                    ELSE
                        INSERT INTO TimedValue VALUES (@TimeStamp, @Value, NULL, @AttributeId, '9999-12-31 23:59:59.997', 0);";
            Console.Write(attributeId);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@TimeStamp", System.Data.SqlDbType.DateTime).Value = timeStamp.ToUniversalTime();
                cmd.Parameters.Add("@AttributeId", System.Data.SqlDbType.UniqueIdentifier).Value = attributeId;
                cmd.Parameters.Add("@Value", System.Data.SqlDbType.NVarChar, 128).Value = value;
                Console.Write(cmd.Transaction);
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public void UpdateLastSQLValue(Guid attributeId, string? value, DateTime timeStamp)
        {
            string query = @"IF EXISTS(SELECT TOP(1)* FROM TimedValue WHERE ElementAttributeId = @attributeId ORDER BY Time DESC) " +
            "UPDATE TimedValue SET Value = @value " +
            "WHERE ElementAttributeId = @attributeId AND Time = " +
            "(SELECT TOP(1)Time FROM TimedValue WHERE ElementAttributeId = @attributeId ORDER BY Time DESC) " +
            "ELSE " +
            "INSERT INTO TimedValue VALUES (@TimeStamp, @value, NULL, @attributeId, '9999-12-31 23:59:59.997', 0);";
            Console.Write(attributeId);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@TimeStamp", System.Data.SqlDbType.DateTime).Value = timeStamp.ToUniversalTime();
                cmd.Parameters.Add("@AttributeId", System.Data.SqlDbType.UniqueIdentifier).Value = attributeId;
                cmd.Parameters.Add("@Value", System.Data.SqlDbType.NVarChar, 128).Value = value;
                Console.Write(cmd.Transaction);
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
    }
}
