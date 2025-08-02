# Musoq: Comprehensive SQL Syntax Documentation

## Introduction

Musoq is a powerful SQL-like query engine that brings the familiarity of SQL to diverse data sources without requiring a traditional database. This comprehensive documentation covers all syntax elements, constructs, and features supported by Musoq, enabling you to query anything from filesystems and Git repositories to code structures and AI models using familiar SQL syntax.

**Key Principles:**
- **One query language for everything** - Use SQL syntax across all data sources
- **Read-only by design** - Focus on querying and analysis, not data modification  
- **Developer-friendly** - Pragmatic syntax extensions that simplify complex tasks
- **Strongly typed** - All queries are strictly typed with compile-time validation

## Table of Contents

### 1. Core SQL Syntax Elements
- [1.1 Basic Query Structure](./basic-query-structure.md)
- [1.2 SELECT Clause](./select-clause.md)
- [1.3 FROM Clause and Data Sources](./from-clause-data-sources.md)
- [1.4 WHERE Clause and Filtering](./where-clause-filtering.md)
- [1.5 ORDER BY Clause and Sorting](./order-by-clause-sorting.md)
- [1.6 GROUP BY Clause and Aggregation](./group-by-clause-aggregation.md)
- [1.7 HAVING Clause](./having-clause.md)

### 2. Advanced Query Constructs
- [2.1 JOIN Operations](./join-operations.md)
  - Inner Joins
  - Cross Apply
  - Outer Apply
- [2.2 Common Table Expressions (CTEs)](./common-table-expressions.md)
- [2.3 Set Operations](./set-operations.md)
  - UNION
  - EXCEPT  
  - INTERSECT
- [2.4 Subqueries and Nested Queries](./subqueries-nested-queries.md)

### 3. Musoq-Specific Syntax Extensions
- [3.1 Schema and Data Source Syntax](./schema-data-source-syntax.md)
- [3.2 Table Definitions](./table-definitions.md)
- [3.3 Coupling Syntax](./coupling-syntax.md)
- [3.4 Cross Apply and Outer Apply](./cross-outer-apply.md)
- [3.5 SKIP and TAKE (Pagination)](./skip-take-pagination.md)
- [3.6 DESC Command for Schema Discovery](./desc-command-schema-discovery.md)

### 4. Data Types and Type System
- [4.1 Supported Data Types](./supported-data-types.md)
- [4.2 Type Conversion and Casting](./type-conversion-casting.md)
- [4.3 Type Inference](./type-inference.md)
- [4.4 Nullable Types](./nullable-types.md)

### 5. Expressions and Operators
- [5.1 Arithmetic Expressions](./arithmetic-expressions.md)
- [5.2 Comparison Operators](./comparison-operators.md)
- [5.3 Logical Operators](./logical-operators.md)
- [5.4 Bitwise Operations](./bitwise-operations.md)
- [5.5 String Operations](./string-operations.md)
- [5.6 Date and Time Operations](./date-time-operations.md)

### 6. Built-in Functions
- [6.1 Aggregate Functions](./aggregate-functions.md)
- [6.2 String Functions](./string-functions.md)
- [6.3 Mathematical Functions](./mathematical-functions.md)
- [6.4 Date and Time Functions](./date-time-functions.md)
- [6.5 Type Conversion Functions](./type-conversion-functions.md)
- [6.6 Conditional Functions](./conditional-functions.md)

### 7. Control Flow and Conditional Logic
- [7.1 CASE WHEN Expressions](./case-when-expressions.md)
- [7.2 NULL Handling](./null-handling.md)
- [7.3 IN and NOT IN Operators](./in-not-in-operators.md)

### 8. Data Source Integration
- [8.1 File System Data Sources](./filesystem-data-sources.md)
- [8.2 Git Repository Data Sources](./git-repository-data-sources.md)
- [8.3 Code Analysis Data Sources](./code-analysis-data-sources.md)
- [8.4 AI and Machine Learning Integration](./ai-ml-integration.md)
- [8.5 Database Connectivity](./database-connectivity.md)
- [8.6 Custom Data Source Development](./custom-data-source-development.md)

### 9. Advanced Features
- [9.1 Regular Expression Support](./regex-support.md)
- [9.2 JSON Path Extraction](./json-path-extraction.md)
- [9.3 Dynamic Schema Handling](./dynamic-schema-handling.md)
- [9.4 Error Handling and Debugging](./error-handling-debugging.md)
- [9.5 Performance Optimization](./performance-optimization.md)

### 10. Best Practices and Patterns
- [10.1 Query Design Patterns](./query-design-patterns.md)
- [10.2 Performance Best Practices](./performance-best-practices.md)
- [10.3 Error Prevention](./error-prevention.md)
- [10.4 Code Style and Conventions](./code-style-conventions.md)

### 11. Practical Examples and Use Cases
- [11.1 File System Analysis](./examples-filesystem-analysis.md)
- [11.2 Git Repository Insights](./examples-git-insights.md)
- [11.3 Code Quality Analysis](./examples-code-quality.md)
- [11.4 Data Transformation Tasks](./examples-data-transformation.md)
- [11.5 AI-Enhanced Analysis](./examples-ai-analysis.md)

### 12. Reference
- [12.1 Complete Syntax Reference](./complete-syntax-reference.md)
- [12.2 Function Reference](./function-reference.md)
- [12.3 Data Source Reference](./data-source-reference.md)
- [12.4 Error Messages Reference](./error-messages-reference.md)
- [12.5 Migration Guide](./migration-guide.md)

## Getting Started

If you're new to Musoq, we recommend starting with:

1. **[Basic Query Structure](./basic-query-structure.md)** - Learn the fundamentals
2. **[FROM Clause and Data Sources](./from-clause-data-sources.md)** - Understand how to connect to data
3. **[Practical Examples](./examples-filesystem-analysis.md)** - See real-world use cases

For experienced SQL users, jump to:
- **[Musoq-Specific Syntax Extensions](./schema-data-source-syntax.md)** - Learn what makes Musoq unique
- **[Advanced Query Constructs](./join-operations.md)** - Leverage advanced features

## Documentation Standards

This documentation follows accessibility and usability best practices:

- **Progressive disclosure** - Information is layered from basic to advanced
- **Task-oriented organization** - Content is structured around what you want to accomplish
- **Consistent terminology** - Technical terms are defined and used consistently
- **Comprehensive examples** - Every concept includes practical, working examples
- **Cross-references** - Related topics are linked for easy navigation

## Contributing

Found an error or want to improve the documentation? See our [contribution guidelines](./contributing.md) for how to help make these docs better.

---

*This documentation covers Musoq's complete syntax and feature set. Each section builds upon previous concepts, so following the suggested reading order will provide the most comprehensive understanding.*