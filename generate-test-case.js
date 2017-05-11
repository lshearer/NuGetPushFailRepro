#!/usr/bin/env node

var fs = require('fs');
var path = require('path');

var src = path.join(__dirname, "src/NugetPushIssueRepro/StaticAssets/IssueTestClass.cs.tmpl");
var dest = path.join(__dirname, "src/NugetPushIssueRepro/StaticAssets/IssueTestClass.cs");

var contents = fs.readFileSync(src, 'utf8');
var code = new Array(100).fill("").map((val, i) => `Console.WriteLine("${i}");`).join('\n            ');

contents = contents.replace('// CODE', code);

fs.writeFileSync(dest, contents);