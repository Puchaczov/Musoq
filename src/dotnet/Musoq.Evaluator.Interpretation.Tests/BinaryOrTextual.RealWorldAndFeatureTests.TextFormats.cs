using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRealWorldAndFeatureTests
{
    #region Real-World Text Format Tests

    /// <summary>
    ///     Tests parsing of key=value configuration format (simpler than Apache logs).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleConfig_ShouldParseKeyValuePairs()
    {
        var query = (
            "\n"
            + "            text Config {\n"
            + "                Key: until '=',\n"
            + "                Value: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                c.Key,\n"
            + "                c.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<Config>(f.Text) c"
        );

        var configLines = new[] { "host=localhost", "port=5432", "database=myapp", "user=admin" };

        var entities = configLines.Select((line, i) => new TextEntity { Name = $"config_{i}", Text = line }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Key", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["host", "localhost"],
            ["port", "5432"],
            ["database", "myapp"],
            ["user", "admin"]
        );
    }

    /// <summary>
    ///     Tests parsing of colon-separated format like /etc/passwd.
    ///     Format: username:password:uid:gid:gecos:home:shell
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_ColonSeparated_ShouldParseUserEntries()
    {
        var query = (
            "\n"
            + "            text PasswdEntry {\n"
            + "                Username: until ':',\n"
            + "                Password: until ':',\n"
            + "                Uid: until ':',\n"
            + "                Gid: until ':',\n"
            + "                Gecos: until ':',\n"
            + "                HomeDir: until ':',\n"
            + "                Shell: rest\n"
            + "            };\n"
            + "            select\n"
            + "                p.Username,\n"
            + "                p.Uid,\n"
            + "                p.Gid,\n"
            + "                p.HomeDir,\n"
            + "                p.Shell\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<PasswdEntry>(f.Text) p\n"
            + "            where p.Uid <> '65534'"
        );

        var passwdLines = new[]
        {
            "root:x:0:0:root:/root:/bin/bash",
            "daemon:x:1:1:daemon:/usr/sbin:/usr/sbin/nologin",
            "www-data:x:33:33:www-data:/var/www:/usr/sbin/nologin",
            "nobody:x:65534:65534:nobody:/nonexistent:/usr/sbin/nologin",
            "developer:x:1000:1000:Developer Account:/home/developer:/bin/zsh",
        };

        var entities = passwdLines.Select((line, i) => new TextEntity { Name = $"passwd_{i}", Text = line }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Username", typeof(string)),
            ("Uid", typeof(string)),
            ("Gid", typeof(string)),
            ("HomeDir", typeof(string)),
            ("Shell", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["root", "0", "0", "/root", "/bin/bash"],
            ["daemon", "1", "1", "/usr/sbin", "/usr/sbin/nologin"],
            ["www-data", "33", "33", "/var/www", "/usr/sbin/nologin"],
            ["developer", "1000", "1000", "/home/developer", "/bin/zsh"]
        );
    }

    /// <summary>
    ///     Tests parsing of pipe-separated log format.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_PipeSeparated_ShouldParseLogEntries()
    {
        var query = (
            "\n"
            + "            text PipeLog {\n"
            + "                Timestamp: until '|',\n"
            + "                Level: until '|',\n"
            + "                Component: until '|',\n"
            + "                Message: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                l.Timestamp,\n"
            + "                l.Level,\n"
            + "                l.Component,\n"
            + "                l.Message\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<PipeLog>(f.Text) l\n"
            + "            where l.Level = 'ERROR'"
        );

        var logLines = new[]
        {
            "2024-01-05 10:30:00|INFO|WebServer|Request received from 10.0.0.1",
            "2024-01-05 10:30:01|ERROR|Database|Connection timeout after 30s",
            "2024-01-05 10:30:02|DEBUG|Cache|Cache miss for key user_123",
            "2024-01-05 10:30:03|ERROR|Auth|Invalid token for user admin",
        };

        var entities = logLines.Select((line, i) => new TextEntity { Name = $"log_{i}", Text = line }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Timestamp", typeof(string)),
            ("Level", typeof(string)),
            ("Component", typeof(string)),
            ("Message", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["2024-01-05 10:30:01", "ERROR", "Database", "Connection timeout after 30s"],
            ["2024-01-05 10:30:03", "ERROR", "Auth", "Invalid token for user admin"]
        );
    }

    /// <summary>
    ///     Tests parsing of HTTP headers (simple Name: Value format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_HttpHeaders_ShouldParseRequestLine()
    {
        var query = (
            "\n"
            + "            text HttpHeader {\n"
            + "                Name: until ':',\n"
            + "                _: until ' ',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select\n"
            + "                h.Name,\n"
            + "                h.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<HttpHeader>(f.Text) h\n"
            + "            where h.Name in ('Content-Type', 'Authorization', 'User-Agent')"
        );

        var headers = new[]
        {
            "Host: api.example.com",
            "Content-Type: application/json",
            "Authorization: Bearer eyJhbGciOiJIUzI1NiIs",
            "User-Agent: MyApp/1.0.0",
            "Accept: */*",
            "Content-Length: 256",
        };

        var entities = headers.Select((h, i) => new TextEntity { Name = $"header_{i}", Text = h }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Content-Type", "application/json"],
            ["Authorization", "Bearer eyJhbGciOiJIUzI1NiIs"],
            ["User-Agent", "MyApp/1.0.0"]
        );
    }

    /// <summary>
    ///     Tests parsing of space-separated fixed-width fields.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SpaceSeparated_ShouldParseData()
    {
        var query = (
            "\n"
            + "            text DataEntry {\n"
            + "                Id: until ' ',\n"
            + "                Name: until ' ',\n"
            + "                Value: until ' ',\n"
            + "                Status: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                d.Id,\n"
            + "                d.Name,\n"
            + "                d.Value,\n"
            + "                d.Status\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<DataEntry>(f.Text) d"
        );

        var dataLines = new[]
        {
            "001 Alpha 100 Active",
            "002 Beta 200 Pending",
            "003 Gamma 300 Complete",
            "004 Delta 400 Failed",
        };

        var entities = dataLines.Select((line, i) => new TextEntity { Name = $"data_{i}", Text = line }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(string)),
            ("Name", typeof(string)),
            ("Value", typeof(string)),
            ("Status", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001", "Alpha", "100", "Active"],
            ["002", "Beta", "200", "Pending"],
            ["003", "Gamma", "300", "Complete"],
            ["004", "Delta", "400", "Failed"]
        );
    }

    /// <summary>
    ///     Tests parsing of tab-separated values (TSV format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TabSeparated_ShouldParseTsvData()
    {
        var query = (
            "\n"
            + "            text TsvRow {\n"
            + "                Name: until '\\t',\n"
            + "                Age: until '\\t',\n"
            + "                City: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                t.Name,\n"
            + "                t.Age,\n"
            + "                t.City\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<TsvRow>(f.Text) t"
        );

        var tsvLines = new[] { "Alice\t30\tNew York", "Bob\t25\tLos Angeles", "Charlie\t35\tChicago" };

        var entities = tsvLines.Select((line, i) => new TextEntity { Name = $"tsv_{i}", Text = line }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Age", typeof(string)),
            ("City", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "30", "New York"],
            ["Bob", "25", "Los Angeles"],
            ["Charlie", "35", "Chicago"]
        );
    }

    /// <summary>
    ///     Tests parsing of semicolon-separated format (like CSV with semicolons).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SemicolonSeparated_ShouldParseCsvData()
    {
        var query = (
            "\n"
            + "            text SemicolonCsv {\n"
            + "                ProductId: until ';',\n"
            + "                ProductName: until ';',\n"
            + "                Price: until ';',\n"
            + "                Quantity: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                c.ProductId,\n"
            + "                c.ProductName,\n"
            + "                c.Price,\n"
            + "                c.Quantity\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<SemicolonCsv>(f.Text) c\n"
            + "            where c.ProductName in ('Laptop', 'Keyboard', 'Monitor')"
        );

        var csvLines = new[]
        {
            "P001;Laptop;999.99;10",
            "P002;Mouse;29.99;150",
            "P003;Keyboard;79.99;75",
            "P004;USB Cable;9.99;500",
            "P005;Monitor;299.99;25",
        };

        var entities = csvLines.Select((line, i) => new TextEntity { Name = $"csv_{i}", Text = line }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ProductId", typeof(string)),
            ("ProductName", typeof(string)),
            ("Price", typeof(string)),
            ("Quantity", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["P001", "Laptop", "999.99", "10"],
            ["P003", "Keyboard", "79.99", "75"],
            ["P005", "Monitor", "299.99", "25"]
        );
    }

    /// <summary>
    ///     Tests parsing of git log oneline format (hash + message).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_GitLogOneline_ShouldParseCommits()
    {
        var query = (
            "\n"
            + "            text GitCommit {\n"
            + "                Hash: until ' ',\n"
            + "                Message: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                g.Hash,\n"
            + "                g.Message\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<GitCommit>(f.Text) g\n"
            + "            where g.Message like '%Fix%' or g.Message like '%Bug%'\n"
            + "            order by g.Hash desc"
        );

        var gitLog = new[]
        {
            "a1b2c3d4 Add new feature for user authentication",
            "e5f6a7b8 Fix null pointer exception in parser",
            "c9d0e1f2 Update dependencies to latest versions",
            "a3b4c5d6 Bug fix handle empty input gracefully",
            "e7f8a9b0 Refactor database connection pooling",
        };

        var entities = gitLog.Select((line, i) => new TextEntity { Name = $"commit_{i}", Text = line }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Hash", typeof(string)), ("Message", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["e5f6a7b8", "Fix null pointer exception in parser"],
            ["a3b4c5d6", "Bug fix handle empty input gracefully"]
        );
    }

    /// <summary>
    ///     Tests parsing of simple CSV format (comma-separated).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleCsv_ShouldParseFields()
    {
        var query = (
            "\n"
            + "            text CsvRow {\n"
            + "                Name: until ',',\n"
            + "                Address: until ',',\n"
            + "                Age: until ',',\n"
            + "                Salary: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                c.Name,\n"
            + "                c.Address,\n"
            + "                c.Age,\n"
            + "                c.Salary\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<CsvRow>(f.Text) c"
        );

        var csvLines = new[]
        {
            "John Smith,123 Main St,35,75000",
            "Jane Doe,456 Oak Ave,28,82000",
            "Bob Wilson,789 Pine Rd,42,95000",
        };

        var entities = csvLines.Select((line, i) => new TextEntity { Name = $"row_{i}", Text = line }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Address", typeof(string)),
            ("Age", typeof(string)),
            ("Salary", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["John Smith", "123 Main St", "35", "75000"],
            ["Jane Doe", "456 Oak Ave", "28", "82000"],
            ["Bob Wilson", "789 Pine Rd", "42", "95000"]
        );
    }

    /// <summary>
    ///     Tests parsing of email-style headers (Name: Value format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_EmailHeaders_ShouldParseMailFields()
    {
        var query = (
            "\n"
            + "            text EmailHeader {\n"
            + "                Field: until ':',\n"
            + "                _: until ' ',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select\n"
            + "                e.Field,\n"
            + "                e.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<EmailHeader>(f.Text) e\n"
            + "            where e.Field in ('From', 'To', 'Subject', 'Date')"
        );

        var emailHeaders = new[]
        {
            "From: sender@example.com",
            "To: recipient@example.com",
            "Subject: Important Meeting Tomorrow",
            "Date: Mon 5 Jan 2026 10:30:00",
            "Message-ID: abc123@mail.example.com",
            "MIME-Version: 1.0",
            "Content-Type: text/plain",
        };

        var entities = emailHeaders.Select((h, i) => new TextEntity { Name = $"header_{i}", Text = h }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Field", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["From", "sender@example.com"],
            ["To", "recipient@example.com"],
            ["Subject", "Important Meeting Tomorrow"],
            ["Date", "Mon 5 Jan 2026 10:30:00"]
        );
    }

    /// <summary>
    ///     Tests parsing of URL-like format (protocol://host/path).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_UrlFormat_ShouldParseUrlComponents()
    {
        var query = (
            "\n"
            + "            text UrlEntry {\n"
            + "                Protocol: until ':',\n"
            + "                _: until '/',\n"
            + "                _: until '/',\n"
            + "                Host: until '/',\n"
            + "                Path: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                u.Protocol,\n"
            + "                u.Host,\n"
            + "                u.Path\n"
            + "            from #test.files() f\n"
            + "            cross apply Parse<UrlEntry>(f.Text) u\n"
            + "            where u.Protocol = 'https'"
        );

        var urls = new[]
        {
            "https://api.example.com/v1/users",
            "http://localhost/health",
            "https://cdn.example.net/assets/image.png",
            "ftp://files.example.org/pub/file.zip",
        };

        var entities = urls.Select((url, i) => new TextEntity { Name = $"url_{i}", Text = url }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Protocol", typeof(string)),
            ("Host", typeof(string)),
            ("Path", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["https", "api.example.com", "v1/users"],
            ["https", "cdn.example.net", "assets/image.png"]
        );
    }

    #endregion
}
