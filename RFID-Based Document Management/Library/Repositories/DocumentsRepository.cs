﻿using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RFID_Based_Document_Management.Library.Database;
using RFID_Based_Document_Management.Library.Models;
using RFID_Based_Document_Management.Library.Services;
using RFID_Based_Document_Management.Library.Services.Parsers;
using MySql.Data.MySqlClient;
namespace RFID_Based_Document_Management.Library.Repositories
{
    class DocumentsRepository
    {
        private MySqlConnection connection;
        private DocumentStatusParser parser;
        private FoldersRepository foldersRepository;
        public DocumentsRepository(DatabaseConnection connection, DocumentStatusParser parser,FoldersRepository foldersRepository)
        {

            this.connection = connection.get();
            this.parser = parser;
            this.foldersRepository = foldersRepository;

        }
        public bool isExistingTag(string tag)
        {
            this.connection.Open();
            string sql = "SELECT COUNT(*) FROM documents WHERE tag=@tag";
            MySqlCommand command = new MySqlCommand(sql, this.connection);
            command.Parameters.AddWithValue("@tag", tag);
            object result = command.ExecuteScalar();
            this.connection.Close();
            int count = 0;
            if (result != null)
            {
                count = Convert.ToInt32(result);
            }

            if (count <= 0)
            {
                return false;
            }

            return true;
        }


        public void save(Document document)
        {
            if(this.isExistingTag(document.Tag))
            {
                throw new Exception("Tag already exist!");
            }

            this.connection.Open();
            string sql="INSERT INTO documents(tag,folder_id,owner,doc_date,status) VALUES(@tag,@folderId,@owner,@docDate,@status)";
            MySqlCommand command = new MySqlCommand(sql, this.connection);
            command.Parameters.AddWithValue("@tag", document.Tag);
            command.Parameters.AddWithValue("@folderId", document.Folder.Id);
            command.Parameters.AddWithValue("@owner", document.Owner);
            command.Parameters.AddWithValue("@docDate", document.Date);
            command.Parameters.AddWithValue("@status", document.Status.ToString());
            command.ExecuteNonQuery();
            this.connection.Close();
        }

        private Document buildDocument(MySqlDataReader reader,Folder folder)
        {
            return new Document(reader[0].ToString(),
                               reader[2].ToString(),
                               StringHelper.removeTimeFromDate(reader[3].ToString()),
                               folder,
                               status: this.parser.parseString(reader[4].ToString()));
        }


        private ArrayList buildDocuments(MySqlDataReader reader, Folder folder)
        {
            ArrayList documents = new ArrayList();
            while (reader.Read())
            {

                documents.Add(this.buildDocument(reader, folder));
            }
            return documents;
        }
        private MySqlCommand selectDocuments(string additionalSql="")
        {
            return new MySqlCommand("SELECT * FROM documents " +additionalSql, this.connection);
        }

        public ArrayList getAll()
        {
            this.connection.Open();
            MySqlCommand command = this.selectDocuments();
            MySqlDataReader reader = command.ExecuteReader();
      
            ArrayList documents = new ArrayList();
            while (reader.Read())
            {

                documents.Add(this.buildDocument(reader, foldersRepository.getFolderById(reader[1].ToString())));
            }
            this.connection.Close();
            return documents;
        }

        public ArrayList getAllFromFolder(Folder folder)
        {
            this.connection.Open();
          
            MySqlCommand command = this.selectDocuments("WHERE folder_id=@folderId");
            command.Parameters.AddWithValue("@folderId", folder.Id);
            MySqlDataReader reader = command.ExecuteReader();
            ArrayList documents = this.buildDocuments(reader,folder);
            this.connection.Close();
            return documents;
        }

        public Document getByTag(string tag)
        {
            this.connection.Open();
            MySqlCommand command = this.selectDocuments("WHERE tag=@tag");
            command.Parameters.AddWithValue("@tag", tag);
            MySqlDataReader reader = command.ExecuteReader();
            Document document=null;
            while (reader.Read())
            {

                document= this.buildDocument(reader, foldersRepository.getFolderById(reader[1].ToString()));
            }
            this.connection.Close();

            return document;

        }

        public void updateStatus(string documentTag,DocumentStatus status)
        {
            this.connection.Open();
            string sql = "UPDATE documents SET status=@status WHERE tag=@tag";
            MySqlCommand command = new MySqlCommand(sql, this.connection);
            command.Parameters.AddWithValue("@status", status.ToString());
            command.Parameters.AddWithValue("@tag", documentTag);
            command.ExecuteNonQuery();
            this.connection.Close();

        }
        public void updateFolderId(string documentTag,string folderId)
        {
            this.connection.Open();
            string sql = "UPDATE documents SET folder_id=@folderId WHERE tag=@tag";
            MySqlCommand command = new MySqlCommand(sql, this.connection);
            command.Parameters.AddWithValue("@folderId",folderId );
            command.Parameters.AddWithValue("@tag", documentTag);
            command.ExecuteNonQuery();
            this.connection.Close();

        }
    }
}
