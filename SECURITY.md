# Security Guidelines for .NET POCO Rules Finder

This document provides security-related information for users and contributors of the **.NET POCO Rules Finder**.  

---

## ⚠️ Important Security Considerations

1. **Administrative Privileges**
   - The tool requires **administrator rights** to attach to other processes and collect ETW events.
   - Only run the tool on trusted machines and processes.
   - Avoid using it on production servers without proper approvals.

2. **Process Access**
   - The tool can monitor any running .NET process.
   - Ensure you have permission to inspect the target applications.

3. **Captured Data**
   - The tool captures runtime method names, classes, and optionally call stacks.
   - **All captured data remains on the same machine** and is **not uploaded to any external server**.
   - This information may contain sensitive application logic or business rules—handle it securely and **avoid sharing publicly**.


4. **Configuration File (`appsettings.json`)**
   - Contains filter settings for processes and methods.

5. **Third-Party Dependencies**
   - The tool depends on:
     - `Microsoft.Diagnostics.Tracing.TraceEvent`
     - .NET runtime libraries

---

## 🔐 Safe Usage Guidelines

- Always run the tool in a controlled environment first before using in production.
- Limit access to the tool and the captured output to authorized personnel.

---

## 🛡️ Security Best Practices for Contributors
- Report security vulnerabilities responsibly via GitHub issues or pull requests.

---

## 📝 Reporting Vulnerabilities

If you discover a security issue or vulnerability:
- Please **report it directly in this GitHub repository** by opening a new issue.
- Provide a detailed description, steps to reproduce, and affected components.
- Do **not post sensitive vulnerability details publicly** outside GitHub.

---

## 📌 Disclaimer

This tool is intended for **diagnostic and monitoring purposes**.  
Users are responsible for using it in accordance with their organization’s **security policies and compliance requirements**.  
