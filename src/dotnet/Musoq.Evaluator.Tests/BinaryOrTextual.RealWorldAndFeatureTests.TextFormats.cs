using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

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
        var query = @"
            text Config {
                Key: until '=',
                Value: rest trim
            };
            select
                c.Key,
                c.Value
            from #test.files() f
            cross apply Parse<Config>(f.Text) c";

        var configLines = new[]
        {
            "host=localhost",
            "port=5432",
            "database=myapp",
            "user=admin"
        };

        var entities = configLines.Select((line, i) => new TextEntity
        {
            Name = $"config_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.Key", typeof(string)),
            ("c.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["host", "localhost"],
            ["port", "5432"],
            ["database", "myapp"],
            ["user", "admin"]);
    }

    /// <summary>
    ///     Tests parsing of colon-separated format like /etc/passwd.
    ///     Format: username:password:uid:gid:gecos:home:shell
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_ColonSeparated_ShouldParseUserEntries()
    {
        var query = @"
            text PasswdEntry {
                Username: until ':',
                Password: until ':',
                Uid: until ':',
                Gid: until ':',
                Gecos: until ':',
                HomeDir: until ':',
                Shell: rest
            };
            select
                p.Username,
                p.Uid,
                p.Gid,
                p.HomeDir,
                p.Shell
            from #test.files() f
            cross apply Parse<PasswdEntry>(f.Text) p
            where p.Uid <> '65534'";

        var passwdLines = new[]
        {
            "root:x:0:0:root:/root:/bin/bash",
            "daemon:x:1:1:daemon:/usr/sbin:/usr/sbin/nologin",
            "www-data:x:33:33:www-data:/var/www:/usr/sbin/nologin",
            "nobody:x:65534:65534:nobody:/nonexistent:/usr/sbin/nologin",
            "developer:x:1000:1000:Developer Account:/home/developer:/bin/zsh"
        };

        var entities = passwdLines.Select((line, i) => new TextEntity
        {
            Name = $"passwd_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Username", typeof(string)),
            ("p.Uid", typeof(string)),
            ("p.Gid", typeof(string)),
            ("p.HomeDir", typeof(string)),
            ("p.Shell", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["root", "0", "0", "/root", "/bin/bash"],
            ["daemon", "1", "1", "/usr/sbin", "/usr/sbin/nologin"],
            ["www-data", "33", "33", "/var/www", "/usr/sbin/nologin"],
            ["developer", "1000", "1000", "/home/developer", "/bin/zsh"]);
    }

    /// <summary>
    ///     Tests parsing of pipe-separated log format.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_PipeSeparated_ShouldParseLogEntries()
    {
        var query = @"
            text PipeLog {
                Timestamp: until '|',
                Level: until '|',
                Component: until '|',
                Message: rest trim
            };
            select
                l.Timestamp,
                l.Level,
                l.Component,
                l.Message
            from #test.files() f
            cross apply Parse<PipeLog>(f.Text) l
            where l.Level = 'ERROR'";

        var logLines = new[]
        {
            "2024-01-05 10:30:00|INFO|WebServer|Request received from 10.0.0.1",
            "2024-01-05 10:30:01|ERROR|Database|Connection timeout after 30s",
            "2024-01-05 10:30:02|DEBUG|Cache|Cache miss for key user_123",
            "2024-01-05 10:30:03|ERROR|Auth|Invalid token for user admin"
        };

        var entities = logLines.Select((line, i) => new TextEntity
        {
            Name = $"log_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("l.Timestamp", typeof(string)),
            ("l.Level", typeof(string)),
            ("l.Component", typeof(string)),
            ("l.Message", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["2024-01-05 10:30:01", "ERROR", "Database", "Connection timeout after 30s"],
            ["2024-01-05 10:30:03", "ERROR", "Auth", "Invalid token for user admin"]);
    }

    /// <summary>
    ///     Tests parsing of HTTP headers (simple Name: Value format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_HttpHeaders_ShouldParseRequestLine()
    {
        var query = @"
            text HttpHeader {
                Name: until ':',
                _: until ' ',
                Value: rest
            };
            select
                h.Name,
                h.Value
            from #test.files() f
            cross apply Parse<HttpHeader>(f.Text) h
            where h.Name in ('Content-Type', 'Authorization', 'User-Agent')";

        var headers = new[]
        {
            "Host: api.example.com",
            "Content-Type: application/json",
            "Authorization: Bearer eyJhbGciOiJIUzI1NiIs",
            "User-Agent: MyApp/1.0.0",
            "Accept: */*",
            "Content-Length: 256"
        };

        var entities = headers.Select((h, i) => new TextEntity
        {
            Name = $"header_{i}",
            Text = h
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("h.Name", typeof(string)),
            ("h.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Content-Type", "application/json"],
            ["Authorization", "Bearer eyJhbGciOiJIUzI1NiIs"],
            ["User-Agent", "MyApp/1.0.0"]);
    }

    /// <summary>
    ///     Tests parsing of space-separated fixed-width fields.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SpaceSeparated_ShouldParseData()
    {
        var query = @"
            text DataEntry {
                Id: until ' ',
                Name: until ' ',
                Value: until ' ',
                Status: rest trim
            };
            select
                d.Id,
                d.Name,
                d.Value,
                d.Status
            from #test.files() f
            cross apply Parse<DataEntry>(f.Text) d";

        var dataLines = new[]
        {
            "001 Alpha 100 Active",
            "002 Beta 200 Pending",
            "003 Gamma 300 Complete",
            "004 Delta 400 Failed"
        };

        var entities = dataLines.Select((line, i) => new TextEntity
        {
            Name = $"data_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("d.Id", typeof(string)),
            ("d.Name", typeof(string)),
            ("d.Value", typeof(string)),
            ("d.Status", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001", "Alpha", "100", "Active"],
            ["002", "Beta", "200", "Pending"],
            ["003", "Gamma", "300", "Complete"],
            ["004", "Delta", "400", "Failed"]);
    }

    /// <summary>
    ///     Tests parsing of tab-separated values (TSV format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TabSeparated_ShouldParseTsvData()
    {
        var query = @"
            text TsvRow {
                Name: until '\t',
                Age: until '\t',
                City: rest trim
            };
            select
                t.Name,
                t.Age,
                t.City
            from #test.files() f
            cross apply Parse<TsvRow>(f.Text) t";

        var tsvLines = new[]
        {
            "Alice\t30\tNew York",
            "Bob\t25\tLos Angeles",
            "Charlie\t35\tChicago"
        };

        var entities = tsvLines.Select((line, i) => new TextEntity
        {
            Name = $"tsv_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("t.Name", typeof(string)),
            ("t.Age", typeof(string)),
            ("t.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "30", "New York"],
            ["Bob", "25", "Los Angeles"],
            ["Charlie", "35", "Chicago"]);
    }

    /// <summary>
    ///     Tests parsing of semicolon-separated format (like CSV with semicolons).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SemicolonSeparated_ShouldParseCsvData()
    {
        var query = @"
            text SemicolonCsv {
                ProductId: until ';',
                ProductName: until ';',
                Price: until ';',
                Quantity: rest trim
            };
            select
                c.ProductId,
                c.ProductName,
                c.Price,
                c.Quantity
            from #test.files() f
            cross apply Parse<SemicolonCsv>(f.Text) c
            where c.ProductName in ('Laptop', 'Keyboard', 'Monitor')";

        var csvLines = new[]
        {
            "P001;Laptop;999.99;10",
            "P002;Mouse;29.99;150",
            "P003;Keyboard;79.99;75",
            "P004;USB Cable;9.99;500",
            "P005;Monitor;299.99;25"
        };

        var entities = csvLines.Select((line, i) => new TextEntity
        {
            Name = $"csv_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.ProductId", typeof(string)),
            ("c.ProductName", typeof(string)),
            ("c.Price", typeof(string)),
            ("c.Quantity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["P001", "Laptop", "999.99", "10"],
            ["P003", "Keyboard", "79.99", "75"],
            ["P005", "Monitor", "299.99", "25"]);
    }

    /// <summary>
    ///     Tests parsing of git log oneline format (hash + message).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_GitLogOneline_ShouldParseCommits()
    {
        var query = @"
            text GitCommit {
                Hash: until ' ',
                Message: rest trim
            };
            select
                g.Hash,
                g.Message
            from #test.files() f
            cross apply Parse<GitCommit>(f.Text) g
            where g.Message like '%Fix%' or g.Message like '%Bug%'
            order by g.Hash desc";

        var gitLog = new[]
        {
            "a1b2c3d4 Add new feature for user authentication",
            "e5f6a7b8 Fix null pointer exception in parser",
            "c9d0e1f2 Update dependencies to latest versions",
            "a3b4c5d6 Bug fix handle empty input gracefully",
            "e7f8a9b0 Refactor database connection pooling"
        };

        var entities = gitLog.Select((line, i) => new TextEntity
        {
            Name = $"commit_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("g.Hash", typeof(string)),
            ("g.Message", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["e5f6a7b8", "Fix null pointer exception in parser"],
            ["a3b4c5d6", "Bug fix handle empty input gracefully"]);
    }

    /// <summary>
    ///     Tests parsing of simple CSV format (comma-separated).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleCsv_ShouldParseFields()
    {
        var query = @"
            text CsvRow {
                Name: until ',',
                Address: until ',',
                Age: until ',',
                Salary: rest trim
            };
            select
                c.Name,
                c.Address,
                c.Age,
                c.Salary
            from #test.files() f
            cross apply Parse<CsvRow>(f.Text) c";

        var csvLines = new[]
        {
            "John Smith,123 Main St,35,75000",
            "Jane Doe,456 Oak Ave,28,82000",
            "Bob Wilson,789 Pine Rd,42,95000"
        };

        var entities = csvLines.Select((line, i) => new TextEntity
        {
            Name = $"row_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.Name", typeof(string)),
            ("c.Address", typeof(string)),
            ("c.Age", typeof(string)),
            ("c.Salary", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["John Smith", "123 Main St", "35", "75000"],
            ["Jane Doe", "456 Oak Ave", "28", "82000"],
            ["Bob Wilson", "789 Pine Rd", "42", "95000"]);
    }

    /// <summary>
    ///     Tests parsing of email-style headers (Name: Value format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_EmailHeaders_ShouldParseMailFields()
    {
        var query = @"
            text EmailHeader {
                Field: until ':',
                _: until ' ',
                Value: rest
            };
            select
                e.Field,
                e.Value
            from #test.files() f
            cross apply Parse<EmailHeader>(f.Text) e
            where e.Field in ('From', 'To', 'Subject', 'Date')";

        var emailHeaders = new[]
        {
            "From: sender@example.com",
            "To: recipient@example.com",
            "Subject: Important Meeting Tomorrow",
            "Date: Mon 5 Jan 2026 10:30:00",
            "Message-ID: abc123@mail.example.com",
            "MIME-Version: 1.0",
            "Content-Type: text/plain"
        };

        var entities = emailHeaders.Select((h, i) => new TextEntity
        {
            Name = $"header_{i}",
            Text = h
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("e.Field", typeof(string)),
            ("e.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["From", "sender@example.com"],
            ["To", "recipient@example.com"],
            ["Subject", "Important Meeting Tomorrow"],
            ["Date", "Mon 5 Jan 2026 10:30:00"]);
    }

    /// <summary>
    ///     Tests parsing of URL-like format (protocol://host/path).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_UrlFormat_ShouldParseUrlComponents()
    {
        var query = @"
            text UrlEntry {
                Protocol: until ':',
                _: until '/',
                _: until '/',
                Host: until '/',
                Path: rest trim
            };
            select
                u.Protocol,
                u.Host,
                u.Path
            from #test.files() f
            cross apply Parse<UrlEntry>(f.Text) u
            where u.Protocol = 'https'";

        var urls = new[]
        {
            "https://api.example.com/v1/users",
            "http://localhost/health",
            "https://cdn.example.net/assets/image.png",
            "ftp://files.example.org/pub/file.zip"
        };

        var entities = urls.Select((url, i) => new TextEntity
        {
            Name = $"url_{i}",
            Text = url
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("u.Protocol", typeof(string)),
            ("u.Host", typeof(string)),
            ("u.Path", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["https", "api.example.com", "v1/users"],
            ["https", "cdn.example.net", "assets/image.png"]);
    }

    #endregion
}
