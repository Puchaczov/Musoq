# Musoq AI Interpretation Schema: Language Extension Specification

**Version:** 0.1.0
**Status:** Proposal  
**Author:** Jakub Puchała

---

## Quick Start

```sql
ai Receipt {
    --- Store or restaurant name
    StoreName: string required,
    --- Total amount paid including tax
    Total: decimal required,
    --- ISO currency code
    Currency: enum('USD', 'EUR', 'PLN')
}

SELECT r.StoreName, r.Total, r.Currency
FROM os.files('./receipts', true) f
CROSS APPLY Infer(f.Base64File(), Receipt) r
ORDER BY r.Total DESC
```

This query extracts structured data from every receipt image in a folder. The `ai` schema defines what to extract — field names, types, and constraints. The `---` comments guide the LLM on what each field means. The `enum` and `required` constraints are validated after extraction and fed back to the model for correction if they fail. The runtime handles which LLM to call, retries, and rate limiting.

**What makes this different from calling an LLM directly:**
- The schema compiles to executable code — type checking, JSON Schema generation, and validation are all generated at compile time, not runtime reflection
- Constraints like `check Total = Subtotal + Tax` catch arithmetic errors that LLMs routinely make
- `when` clauses enable conditional extraction — fields that only exist under certain conditions don't get hallucinated
- The result is a SQL row, so it composes with JOINs, CTEs, GROUP BY, and every other SQL operation — including data from completely different sources

**What it composes with:**

```sql
-- Cross-reference AI-extracted invoices with a bank statement CSV
with Invoices as (
    SELECT inv.Vendor.Name, inv.Total, inv.InvoiceDate
    FROM os.files('./invoices') f
    CROSS APPLY Infer(f.Base64File(), Invoice) inv
),
Payments as (
    SELECT ToDecimal(Amount) as Amount, Description
    FROM separatedvalues.comma('./bank.csv', true, 0)
)
SELECT i.Name, i.Total, p.Amount
FROM Invoices i
INNER JOIN Payments p ON Abs(i.Total - Abs(p.Amount)) < 0.01
```

No other structured extraction tool can JOIN AI-extracted data with CSV files, git commits, or C# code analysis in a single query. That's what `ai` schemas enable when combined with Musoq's existing data sources.

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Design Principles](#2-design-principles)
3. [Core Language Changes](#3-core-language-changes)
4. [AI Schema Syntax](#4-ai-schema-syntax)
5. [Type System](#5-type-system)
6. [Constraint Language](#6-constraint-language)
7. [Inference Functions](#7-inference-functions)
8. [Schema Composition](#8-schema-composition)
9. [Error Handling](#9-error-handling)
10. [Runtime Contract](#10-runtime-contract)
11. [Grammar Specification](#11-grammar-specification)
12. [Examples](#12-examples)
13. [Future Considerations](#13-future-considerations)
14. [Appendix A: Reserved Keywords](#appendix-a-reserved-keywords)
15. [Appendix B: Type Mapping](#appendix-b-type-mapping)
16. [Appendix C: Error Code Summary](#appendix-c-error-code-summary)
17. [Appendix D: Comparison with Existing Approaches](#appendix-d-comparison-with-existing-approaches)

---

## 1. Introduction

### 1.1 Purpose

AI Interpretation Schemas extend Musoq's SQL dialect with declarative structure definitions for extracting typed, validated data from unstructured content using large language models. AI schemas serve triple duty as:

1. **Extraction contracts** — Compiled to typed classes with validation logic, identical in structure to binary/text schemas
2. **Prompt generators** — Schema definitions automatically produce structured output instructions for LLMs; the schema IS the prompt
3. **Format documentation** — Human-readable specifications of expected structure that cannot drift from implementation

### 1.2 Motivation

Musoq's binary and text interpretation schemas solve a clear problem: declaratively describing how to interpret structured data. But many real-world data sources contain information that has no deterministic parsing rule:

- Invoices and receipts (varied layouts, languages, formats)
- Unstructured log messages with human-written context
- Legacy documents with inconsistent formatting
- Images containing embedded text, tables, or diagrams
- Natural language descriptions that encode structured facts

Current approaches require either hand-crafted prompt engineering per use case (fragile, non-composable) or generic LLM calls that return untyped strings (losing all benefits of SQL's type system). AI schemas bring structured AI extraction into the query itself, enabling single-query workflows that extract AND validate AND correlate — the same pattern established by `binary` and `text` schemas.

### 1.3 Scope

This specification covers:

- AI schema definition syntax and semantics
- Type system with constraints, enums, and conditional fields
- Comment system distinguishing prompt content from developer notes
- Schema annotations for runtime metadata (model routing, fallback, privacy)
- Integration with existing Musoq syntax (CROSS APPLY, OUTER APPLY, JOINs, CTEs)
- Schema composition and reuse patterns
- Error handling and validation semantics
- Code generation and compilation of schema artifacts
- Runtime interface contract (inference runner)

This specification does **not** cover:

- LLM provider selection, configuration, or authentication (runtime concern)
- Retry policies, rate limiting, or backoff strategies (runtime concern)
- Caching strategies (runtime concern)
- Parallelism or batching (runtime concern)
- Cost tracking or budgeting (runtime concern)
- Prompt engineering strategies (the schema generates prompts automatically)
- Model fine-tuning or training
- Embedding-based similarity operations (separate future specification)

This specification is part of the Musoq specification family (see *Musoq Core SQL Language Specification*, §1.6). The notation conventions defined in the core specification (§1.5) apply to this document. In particular, the key words MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY carry normative weight when capitalized.

An implementation that claims conformance to the AI Interpretation profile MUST implement all features defined in this specification. This profile is optional; see the core specification (§1.7) for the conformance model.

### 1.4 Terminology

| Term | Definition |
|------|------------|
| **AI Schema** | A named structure definition describing what to extract from unstructured content |
| **Field** | A named element within an AI schema with a type, optional constraints, and optional conditions |
| **Substrate** | The unstructured input being interpreted (text, image as base64, or multimodal content) |
| **Inference** | The act of extracting structured data from a substrate using an LLM |
| **Contract** | The combination of schema definition + generated JSON schema + validation rules that constrain LLM output |
| **Doc comment** | A `---` comment included in the generated prompt as extraction guidance |
| **Code comment** | A `--` or `/* */` comment excluded from the prompt; developer notes only |
| **Annotation** | A `[key: 'value']` metadata pair on a schema, compiled into `InferenceRequest` for runtime use |
| **Runtime** | The host program that implements `IAiInferenceRunner` and handles all LLM communication |

### 1.5 Relationship to Existing Constructs

AI schemas are the third member of the interpretation schema family:

| Schema Type | Input | Interpretation Method | Function | Output |
|-------------|-------|----------------------|----------|--------|
| `binary` | `byte[]` | Byte-level parsing | `Interpret()` | Typed object |
| `text` | `string` | Character-level parsing | `Parse()` | Typed object |
| `ai` | `string` or `byte[]` (base64 image) | LLM inference | `Infer()` | Typed object |

All three:
- Define named structures with typed fields
- Compile to typed classes at query compile time
- Support conditional fields via `when` clauses
- Support validation via `check` clauses
- Compose through schema references
- Integrate with CROSS APPLY / OUTER APPLY
- Follow "define before use" scoping rules

The key difference: `binary` and `text` schemas describe deterministic parsing rules. `ai` schemas describe *what* to extract, leaving the *how* to the LLM. The schema constrains and validates the LLM's output, not the parsing algorithm.

### 1.6 Relationship to TABLE/COUPLE

TABLE/COUPLE provides explicit typing for dynamic data sources. AI schemas solve a different problem: extracting structure from unstructured content. However, they share concepts:

| Concept | TABLE/COUPLE | AI Schema |
|---------|-------------|-----------|
| Type declaration | Column types in TABLE | Field types in `ai` block |
| Binding | COUPLE binds to schema method | `Infer()` delegates to runtime |
| Usage | Coupled alias in FROM clause | CROSS APPLY with `Infer()` |
| Scope | Query batch | Query batch |

AI schemas produce richer contracts (constraints, enums, conditionals) that TABLE definitions do not support, because AI extraction requires tighter output specification to be reliable.

### 1.7 Specification vs Runtime Boundary

This specification defines a clear boundary between what the engine compiles and what the host provides:

```
┌─────────────────────────────────────────────────────┐
│                  SPECIFICATION SCOPE                  │
│                                                       │
│  AI Schema Source Text (with annotations)             │
│       ↓                                               │
│  Parser + Compiler                                    │
│       ↓                                               │
│  Generated Artifacts:                                 │
│    • Typed class (typed properties)                    │
│    • JSON Schema (structural contract)                │
│    • Prompt template (from doc comments + types)      │
│    • Validator (compiled constraint checks)           │
│    • Deserializer (JSON → typed object)               │
│    • Retry prompt generator (from validation errors)  │
│    • Annotations dictionary (from [key: 'value'])     │
│       ↓                                               │
│  Inference runner interface ◄── BOUNDARY            │
│       ↓                                               │
└───────┼─────────────────────────────────────────────┘
        │
┌───────┼─────────────────────────────────────────────┐
│       ↓           HOST/RUNTIME SCOPE                  │
│                                                       │
│  Implementation decides ALL of:                       │
│    • Which LLM to call (informed by annotations)      │
│    • Retry policy                                     │
│    • Rate limiting                                    │
│    • Timeout behavior                                 │
│    • Caching                                          │
│    • Parallelism / batching                           │
│    • Credential management                            │
│    • Logging / telemetry                              │
│                                                       │
└─────────────────────────────────────────────────────┘
```

**Everything above the boundary** is defined by this specification and produced at compile time.  
**Everything below the boundary** is implementation-defined and provided by the host program via the inference runner interface.

---

## 2. Design Principles

### 2.1 The Schema IS the Prompt

The single most important design principle: **the AI schema definition generates the LLM prompt automatically.** Query authors never write prompts. The schema's field names, types, constraints, enums, and doc comments are compiled into structured output instructions.

This ensures:
- The prompt cannot drift from the validation logic (they're generated from the same source)
- Schema changes automatically update the prompt
- No prompt engineering expertise required from query authors
- Consistent extraction behavior across models

### 2.2 Declarative Over Imperative

Consistent with binary/text schemas: describe **what** the data looks like, not **how** to extract it. The runtime determines the extraction strategy (model selection, prompt structuring, retry logic).

### 2.3 Separation of Concerns

The specification compiles schemas into artifacts (classes, validators, prompt templates). The runtime consumes those artifacts to perform inference. The spec never prescribes how the runtime operates — it only defines what the runtime receives and what it must return.

### 2.4 Schemas Are Documentation

AI schema definitions should read as extraction specifications. Doc comments (`---`) are first-class and included in prompt generation. Code comments (`--`, `/* */`) are for developers and never reach the LLM.

### 2.5 Zero Runtime Reflection

Like binary/text schemas, all AI schema definitions compile to statically-typed code. The generated class, JSON schema, prompt template, and validators are all produced at compile time. No runtime type inspection.

---

## 3. Core Language Changes

### 3.1 New Keyword: `ai`

The `ai` keyword introduces an AI schema definition, parallel to `binary` and `text`.

**Placement:** Same rules as binary/text schemas — defined before any query that references them, scoped to the query batch.

```sql
ai Receipt {
    --- Store or restaurant name
    StoreName: string,
    --- Total amount paid
    Total:     decimal
}

SELECT r.StoreName, r.Total
FROM os.files('./receipts', true) f
CROSS APPLY Infer(f.Base64File(), Receipt) r
```

### 3.2 New Interpretation Function: `Infer()`

`Infer()` is the AI equivalent of `Interpret()` (binary) and `Parse()` (text).

**Signatures:**

```
Infer(content, SchemaType)           -- Basic inference
Infer(content, SchemaType, hint)     -- With extraction hint
TryInfer(content, SchemaType)        -- Returns null on failure
TryInfer(content, SchemaType, hint)  -- Safe with hint
```

The function integrates with CROSS APPLY / OUTER APPLY using the same relaxed semantics specified in the binary/text specification (Section 3.2: single complex objects wrapped in single-element arrays).

### 3.3 Scope Rules

Identical to binary/text schemas:

- AI schemas are visible only within the query batch where defined
- Schema names MUST be unique within a batch (across `binary`, `text`, and `ai` namespaces)
- Forward references between schemas MUST NOT occur
- Recursive schema definitions MUST NOT occur
- `ai` schemas may reference other `ai` schemas (composition)

---

## 4. AI Schema Syntax

### 4.1 Schema Declaration

```
AiSchema ::= 'ai' Identifier Annotations? '{' AiFieldList '}'

Annotations ::= '[' AnnotationList ']'

AnnotationList ::= Annotation (',' Annotation)*

Annotation ::= Identifier ':' StringLiteral

AiFieldList ::= AiField (',' AiField)* ','?
```

**Field Separator Rules:**
- Fields are separated by commas
- Trailing comma after the last field is OPTIONAL
- Newlines are NOT significant (formatting only)

Example:

```sql
ai InvoiceHeader {
    --- Name of the vendor or business that issued the invoice
    VendorName:     string,
    --- Invoice identifier (alphanumeric, typically with a prefix)
    InvoiceNumber:  string,
    --- Date the invoice was issued
    InvoiceDate:    datetime,
    --- Date payment is due
    DueDate:        datetime,
    --- Total amount due including tax
    TotalAmount:    decimal,
    --- Three-letter ISO currency code
    Currency:       enum('USD', 'EUR', 'GBP', 'PLN', 'CHF', 'JPY')
}
```

### 4.2 Schema Annotations

Annotations are key-value metadata attached to a schema definition. They are compiled into the `InferenceRequest` and passed through to the runtime. The specification defines the syntax and transport mechanism but **does not define the semantics of any annotation** — interpretation is entirely the runtime's responsibility.

```sql
ai Receipt [tier: 'economy'] {
    --- Store name
    StoreName: string required,
    --- Total amount paid
    Total: decimal required
}

ai SecurityAudit [tier: 'premium', vision: 'required'] {
    --- Whether the code contains hardcoded credentials
    HasHardcodedCredentials: bool,
    --- Overall security risk level
    OverallRisk: enum('safe', 'low', 'medium', 'high', 'critical')
}
```

**Semantics:**
- Annotations are string key-value pairs
- Keys are identifiers (same rules as field names)
- Values are string literals
- Duplicate keys in the same annotation list are a compile-time error
- Annotations are available to the runtime via `InferenceRequest.Annotations`
- The engine does not interpret, validate, or act on annotation values

**Why annotations are schema-level, not call-site:** Annotations describe properties of the extraction *task*, not the content being extracted. An invoice extraction is always an "economy tier" task regardless of which specific invoice is being processed. This keeps the `Infer()` call clean — content and hint are the only per-row parameters.

**Intended usage patterns:**

The spec takes no position on what annotations mean, but common conventions between query authors and runtime implementations might include:

| Annotation | Possible Runtime Interpretation |
|-----------|-------------------------------|
| `tier: 'economy'` | Use cheapest model that can handle the schema complexity |
| `tier: 'premium'` | Use the most capable model available |
| `tier: 'local'` | Use only local/offline models (privacy requirement) |
| `vision: 'required'` | Content will be base64 images; select a vision-capable model |
| `vision: 'preferred'` | Content may contain images; vision model preferred but not required |
| `language: 'pl'` | Primary content language (may inform model selection or prompt localization) |
| `fallback: 'true'` | If primary model fails validation, try a more capable model |
| `batch: 'true'` | This schema will be called on many rows; optimize for throughput |

These are conventions, not part of the specification. A runtime that doesn't recognize an annotation simply ignores it.

**Tiered extraction pattern:**

The `tier` and `fallback` annotations enable the "cheap model first, expensive model if it fails" pattern without leaking model names into the query:

```sql
-- Runtime interprets: try economy model first, fall back to premium on validation failure
ai Receipt [tier: 'economy', fallback: 'true'] {
    --- Store name
    StoreName: string required,
    --- Total paid
    Total: decimal required,
    --- Currency
    Currency: enum('USD', 'EUR', 'PLN')
}

-- Runtime interprets: always use premium model (complex schema, high accuracy needed)
ai Invoice [tier: 'premium'] {
    --- Invoice with full line item extraction and cross-field validation
    Vendor:         Vendor,
    InvoiceNumber:  string required,
    LineItems:      InvoiceLineItem[],
    Total:          decimal required check Total = Subtotal + TaxAmount,
    Subtotal:       decimal,
    TaxAmount:      decimal
}
```

A runtime implementation might handle this as (pseudocode):

```
function Infer(request):
    tier = request.Annotations["tier"] or "standard"
    allowFallback = request.Annotations["fallback"] == "true"
    
    model = select model based on tier:
        "economy"  => "gpt-4o-mini"
        "premium"  => "gpt-4o"
        "local"    => "ollama:llama3"
        _          => defaultModel
    
    result = tryWithModel(model, request)
    
    if not result.IsSuccess and allowFallback and tier != "premium":
        result = tryWithModel("gpt-4o", request)
    
    return result
```

The query author expresses intent ("this extraction is cheap and can fall back"). The runtime translates intent to implementation. Neither side knows about the other's internals.

### 4.3 Comment System

AI schemas support two distinct comment syntaxes with different semantics:

| Syntax | Name | Included in Prompt | Purpose |
|--------|------|-------------------|---------|
| `---` (triple dash) | **Doc comment** | Yes | Field-level extraction guidance for the LLM |
| `--` (double dash) | **Code comment** | No | Developer notes, TODOs, maintenance remarks |
| `/* ... */` | **Block code comment** | No | Multi-line developer notes |

**Rationale:** The triple-dash `---` is visually distinct, easy to type, and doesn't conflict with any existing Musoq syntax. The convention mirrors the distinction between single-line and documentation comments in many languages.

**Disambiguation rule:** A line starting with exactly three dashes `---` followed by a space is a doc comment. A line starting with exactly two dashes `--` is a code comment. Four or more dashes are treated as code comments (only `---` is special).

**Grammar:**

```
DocComment   ::= '---' ' ' (any character except newline)* newline
CodeComment  ::= '--' (any character except newline)* newline
BlockComment ::= '/*' (any character)* '*/'
```

**Example:**

```sql
ai InvoiceHeader {
    -- This schema handles both US and EU invoice formats.
    -- Last reviewed: 2026-02-15 by Jakub

    --- Name of the vendor or business that issued the invoice
    VendorName:     string,

    --- Invoice identifier, typically alphanumeric with a prefix like "INV-" or "FV/"
    InvoiceNumber:  string,

    -- TODO: should we split this into IssueDate and DocumentDate?
    --- Date the invoice was issued
    InvoiceDate:    datetime,

    --- Date payment is due (must be on or after InvoiceDate)
    DueDate:        datetime check DueDate >= InvoiceDate,

    --- Total amount due including all taxes
    TotalAmount:    decimal required,

    /* 
     * We might want to expand this enum later to include
     * more currencies. For now keeping it to what we actually
     * encounter in our invoices.
     */
    --- Three-letter ISO 4217 currency code
    Currency:       enum('USD', 'EUR', 'GBP', 'PLN', 'CHF', 'JPY')
}
```

**What the LLM sees** (generated from the above):

```
Fields:
- VendorName (string): Name of the vendor or business that issued the invoice
- InvoiceNumber (string): Invoice identifier, typically alphanumeric with a prefix like "INV-" or "FV/"
- InvoiceDate (datetime): Date the invoice was issued
- DueDate (datetime): Date payment is due (must be on or after InvoiceDate)
- TotalAmount (decimal, required): Total amount due including all taxes
- Currency (one of: USD, EUR, GBP, PLN, CHF, JPY): Three-letter ISO 4217 currency code
```

The TODO, the date stamp, the multi-line discussion about enum expansion — none of it reaches the model.

#### 4.3.1 Doc Comment Placement Rules

Doc comments are **associated with the field that follows them**. Multiple consecutive doc comments before a field are concatenated:

```sql
ai Receipt {
    --- Name of the store or restaurant
    --- Include the full legal name if visible, not just a brand name
    StoreName: string required
}
```

Generates: `StoreName (string, required): Name of the store or restaurant. Include the full legal name if visible, not just a brand name`

**Orphaned doc comments** (doc comments not followed by a field) are a compile-time warning. They don't cause errors but indicate likely mistakes.

#### 4.3.2 Schema-Level Doc Comments

A doc comment immediately after the opening brace and before the first field becomes the **schema-level description**, included in the prompt as context:

```sql
ai Receipt {
    --- Extract receipt data from a photo of a paper receipt.
    --- The receipt may be crumpled, partially obscured, or in any language.
    
    --- Store or restaurant name
    StoreName: string required,
    --- Total amount paid
    Total: decimal required
}
```

Generates:

```
Context: Extract receipt data from a photo of a paper receipt. The receipt may be crumpled, partially obscured, or in any language.

Fields:
- StoreName (string, required): Store or restaurant name
- Total (decimal, required): Total amount paid
```

#### 4.3.3 Enum Value Doc Comments

Doc comments can annotate individual enum values:

```sql
ai RiskAssessment {
    --- Overall risk level of the assessed item
    Level: enum(
        --- No action required, meets all standards
        'low',
        --- Address within normal maintenance cycle
        'medium',
        --- Requires attention within current sprint/week
        'high',
        --- Stop-the-line severity, immediate action required
        'critical'
    )
}
```

The descriptions are included in the prompt to help the model distinguish between enum values.

### 4.4 Field Declaration

```
AiField ::= (DocComment | CodeComment)* Identifier ':' AiType Modifiers? 
             ConditionClause? CheckClause?

Modifiers ::= Modifier+

ConditionClause ::= 'when' Expression

CheckClause ::= 'check' Expression
```

Each field declaration consists of:

1. **Optional comments** — Doc comments become prompt content; code comments are ignored
2. **Field name** — Case-sensitive identifier
3. **Type** — One of the supported AI types (Section 5)
4. **Optional modifiers** — Type-specific constraints (e.g., `max(200)`, `range(1, 10)`)
5. **Optional condition** — `when` clause for conditional extraction
6. **Optional validation** — `check` clause for post-extraction validation

### 4.5 Discard Fields

Fields named `_` are extracted but not exposed in query results:

```sql
ai Document {
    _:          string,     -- Extraction context consumed but not returned
    Title:      string,
    Author:     string
}
```

### 4.6 Reserved Field Names

| Field | Type | Purpose |
|-------|------|---------|
| `_confidence` | `decimal` | Model-reported confidence score (0.0 to 1.0) |

Reserved fields are **optional** — declare them only if needed:

```sql
ai Receipt {
    StoreName:    string,
    Total:        decimal,
    _confidence:  decimal    -- Request confidence metadata from the model
}
```

When declared, the field is included in the prompt as an additional extraction target. When not declared, no confidence is requested.

**Field name conventions:**
- The single underscore `_` is a discard field (see §4.5)
- Field names prefixed with `_` (e.g., `_confidence`) are reserved for specification-defined metadata. Users MUST NOT define custom fields starting with `_`.

---

## 5. Type System

### 5.1 Primitive Types

| AI Type | .NET Type | JSON Type | Description |
|---------|-----------|-----------|-------------|
| `string` | `string` | `string` | Free-form text |
| `int` | `int?` | `integer` | 32-bit signed integer |
| `long` | `long?` | `integer` | 64-bit signed integer |
| `decimal` | `decimal?` | `number` | Precise decimal number |
| `float` | `float?` | `number` | Single-precision floating point |
| `double` | `double?` | `number` | Double-precision floating point |
| `bool` | `bool?` | `boolean` | True or false |
| `datetime` | `DateTime?` | `string` (ISO 8601) | Date and/or time |
| `date` | `DateOnly?` | `string` (ISO 8601) | Date without time |
| `time` | `TimeOnly?` | `string` (ISO 8601) | Time without date |

**Nullability:**
- Value types (`int`, `decimal`, `bool`, etc.) are **nullable by default** in AI schemas. LLMs may not always extract every field.
- `string` is nullable by default.
- Use `required` modifier to enforce non-null: `Total: decimal required`

### 5.2 Enum Type

```
EnumType ::= 'enum' '(' StringLiteral (',' StringLiteral)+ ')'
```

Enums constrain extraction to a fixed set of values:

```sql
ai Ticket {
    Priority: enum('low', 'medium', 'high', 'critical'),
    Status:   enum('open', 'in_progress', 'resolved', 'closed'),
    Category: enum('bug', 'feature', 'question', 'docs')
}
```

**Semantics:**
- Enum values are case-sensitive strings
- The LLM is instructed to return exactly one of the listed values
- Validation rejects any value not in the enum set
- Enums compile to `string` at the type level but with runtime validation
- At least two values are required

### 5.3 Array Type

```
ArrayType ::= Type '[]'
```

Arrays represent collections of extracted items:

```sql
ai Invoice {
    VendorName: string,
    LineItems:  InvoiceLineItem[],
    Total:      decimal
}

ai InvoiceLineItem {
    Description: string,
    Quantity:    int,
    UnitPrice:   decimal,
    Amount:      decimal
}
```

**Semantics:**
- Arrays are exposed as `IEnumerable<T>` for CROSS APPLY iteration
- Empty arrays are valid (no items found)
- Arrays of primitives are supported: `Tags: string[]`
- Arrays of enums are supported: `Categories: enum('a', 'b', 'c')[]`
- Nested arrays (arrays of arrays) are NOT supported

### 5.4 Schema Reference Type

Fields may reference other AI schemas by name:

```sql
ai Address {
    Street:     string,
    City:       string,
    PostalCode: string,
    Country:    string
}

ai Customer {
    Name:            string,
    BillingAddress:  Address,
    ShippingAddress: Address
}
```

**Semantics:**
- Referenced schema MUST be defined before the referencing schema
- Nested schema fields accessible via dot notation: `customer.BillingAddress.City`
- Composition depth is limited to 4 levels (to bound prompt complexity)

---

## 6. Constraint Language

### 6.1 String Constraints

| Modifier | Syntax | Description |
|----------|--------|-------------|
| Max length | `max(n)` | Maximum character count |
| Min length | `min(n)` | Minimum character count |
| Pattern | `pattern 'regex'` | Must match regex pattern |
| Required | `required` | Must not be null or empty |

```sql
ai Record {
    Name:           string required max(100),
    Email:          string pattern '.+@.+\..+',
    InvoiceNumber:  string pattern '[A-Z]{2,3}-\d{4,8}',
    Notes:          string max(500)
}
```

### 6.2 Numeric Constraints

| Modifier | Syntax | Description |
|----------|--------|-------------|
| Range | `range(min, max)` | Inclusive bounds |
| Required | `required` | Must not be null |

```sql
ai Measurement {
    Temperature:  decimal range(-273.15, 1000000),
    Humidity:     int range(0, 100),
    Pressure:     decimal range(800, 1200),
    SampleCount:  int range(1, 10000) required
}
```

### 6.3 Check Constraints

Post-extraction validation using SQL expressions, identical in syntax to binary schema `check` clauses:

```
CheckClause ::= 'check' Expression
```

```sql
ai InvoiceLine {
    Quantity:   int range(1, 10000),
    UnitPrice:  decimal range(0, 1000000),
    Amount:     decimal check Amount = Quantity * UnitPrice
}

ai DateRange {
    StartDate: datetime required,
    EndDate:   datetime required check EndDate >= StartDate
}
```

**Semantics:**
- Expression is evaluated after all fields are extracted
- Failed check produces a structured `ValidationError` (see Section 9)
- Expression may reference any field in the same schema
- Expression must evaluate to boolean
- Check expressions use SQL operators (same as binary/text schemas)
- Built-in functions available: `Length()`, `Abs()`, `Round()`

### 6.4 Conditional Fields (when clause)

Fields may be conditionally extracted based on other field values:

```
ConditionalField ::= Identifier ':' AiType 'when' Expression
```

```sql
ai MedicalRecord {
    PatientType:    enum('inpatient', 'outpatient', 'emergency'),
    AdmissionDate:  datetime,
    
    --- Room assignment for admitted patients
    RoomNumber:     string when PatientType = 'inpatient',
    DischargeDate:  datetime when PatientType = 'inpatient',
    
    --- Emergency-specific triage information
    TriageLevel:    int range(1, 5) when PatientType = 'emergency',
    ArrivalMethod:  enum('ambulance', 'walk-in', 'transfer') when PatientType = 'emergency'
}
```

**Semantics:**
- Condition is evaluated against extracted field values
- When condition is FALSE: field is not requested from LLM, value is `null`
- When condition is TRUE: field is extracted normally
- Conditions may only reference fields declared BEFORE the conditional field
- The generated prompt includes conditional instructions for the LLM

**Implementation Note:**

Conditional extraction may use a two-phase approach (extract non-conditional first, then conditional) or a single prompt with conditional instructions. The choice is implementation-defined, but the observable semantics MUST be: conditional fields are `null` when their condition is false, regardless of what the LLM returns.

**Null Propagation:**

Same rules as binary schemas. When a conditional field is `null`, any expression referencing it evaluates to `null`:

```sql
ai Order {
    HasDiscount: bool,
    DiscountPercent: decimal range(0, 100) when HasDiscount = true,
    -- If HasDiscount is false, DiscountPercent is null, DiscountAmount is null
    DiscountAmount: decimal when HasDiscount = true
}
```

### 6.5 Constraint Interaction with Validation

When a constraint fails, the compiled validator produces structured errors. These errors are available to the runtime for retry logic (see Section 11):

| Constraint | Error Feedback Example |
|-----------|----------------------|
| `range(1, 10)` | `"Field 'Rating' value 15 is outside range [1, 10]"` |
| `enum('a', 'b')` | `"Field 'Status' value 'unknown' is not one of: a, b"` |
| `check A = B * C` | `"Field 'Amount' check failed: 150.00 ≠ 10 × 14.00 (140.00)"` |
| `pattern '\d+'` | `"Field 'Code' value 'ABC' does not match pattern '\d+'"` |
| `max(100)` | `"Field 'Name' length 142 exceeds maximum 100"` |
| `required` | `"Field 'Total' is required but was null or empty"` |

---

## 7. Inference Functions

### 7.1 Function Signatures

AI inference functions follow the same pattern as `Interpret()` (binary) and `Parse()` (text):

**Primary inference:**

```
Infer(content, SchemaType)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `content` | `string` | Text content or base64-encoded image |
| `SchemaType` | Schema reference | AI schema defined in the query batch |

**Inference with hint:**

```
Infer(content, SchemaType, hint)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `hint` | `string` | Additional extraction context passed through to the runtime |

**Safe inference:**

```
TryInfer(content, SchemaType) -> SchemaType?
TryInfer(content, SchemaType, hint) -> SchemaType?
```

Returns `null` instead of throwing on extraction failure.

**Partial inference (debugging):**

```
PartialInfer(content, SchemaType)
PartialInfer(content, SchemaType, hint)
```

Returns a partial result for debugging extraction issues. Like all inference functions, `PartialInfer` MUST be used inside `CROSS APPLY` or `OUTER APPLY` (see §7.3).

| Field | Type | Description |
|-------|------|-------------|
| `ExtractedFields` | Dictionary | Successfully extracted and validated fields |
| `ValidationErrors` | List | Validation errors with field names and details |
| `RawResponse` | `string` | Raw JSON response before parsing |

**Note:** There is no `provider` or `model` parameter. Model selection is a runtime concern. The runtime receives the inference request and decides which model to use. See Section 11.4 for how hosts can implement model selection.

### 7.2 SQL Usage Patterns

**Basic extraction from text:**

```sql
SELECT r.StoreName, r.Total
FROM os.files('./receipts') f
CROSS APPLY Infer(f.GetFileContent(), Receipt) r
```

**Extraction from images (vision):**

```sql
SELECT r.StoreName, r.Total
FROM os.files('./scanned-receipts', true) f
CROSS APPLY Infer(f.Base64File(), Receipt) r
WHERE f.Extension IN ('.jpg', '.png', '.pdf')
```

**With extraction hint:**

```sql
SELECT r.StoreName, r.Total
FROM os.files('./invoices') f
CROSS APPLY Infer(
    f.GetFileContent(), 
    Invoice, 
    'Polish invoice. Dates in DD.MM.YYYY format. Currency is PLN unless stated.'
) r
```

**Safe extraction with OUTER APPLY:**

```sql
SELECT 
    f.Name,
    CASE WHEN r IS NOT NULL THEN r.Total ELSE NULL END as Total
FROM os.files('./mixed-documents') f
OUTER APPLY TryInfer(f.GetFileContent(), Receipt) r
```

**Array expansion with nested CROSS APPLY:**

```sql
SELECT 
    inv.VendorName,
    inv.InvoiceNumber,
    li.Description,
    li.Quantity,
    li.Amount
FROM os.files('./invoices') f
CROSS APPLY Infer(f.Base64File(), Invoice) inv
CROSS APPLY inv.LineItems li
WHERE inv.TotalAmount > 1000
ORDER BY inv.TotalAmount DESC
```

### 7.3 Interaction with CROSS APPLY / OUTER APPLY

Same relaxed semantics as binary/text interpretation (binary/text spec Section 3.2):

| `Infer()` Result | CROSS APPLY | OUTER APPLY |
|-------------------|-------------|-------------|
| Valid object | 1 row, fields bound to alias | 1 row, fields bound to alias |
| `null` (TryInfer) | 0 rows | 1 row, NULL alias |
| Exception (Infer) | Query fails | Query fails |

---

## 8. Schema Composition

### 8.1 Schema References

AI schemas may reference other AI schemas by name. Referenced schemas MUST be defined first.

```sql
ai Address {
    Street:     string required,
    City:       string required,
    State:      string,
    PostalCode: string required,
    Country:    string required
}

ai ContactInfo {
    Email:  string pattern '.+@.+\..+',
    Phone:  string,
    Fax:    string
}

ai Vendor {
    Name:       string required,
    TaxId:      string,
    Address:    Address,
    Contact:    ContactInfo
}

ai Invoice {
    Vendor:         Vendor,
    InvoiceNumber:  string required,
    Date:           datetime required,
    DueDate:        datetime,
    LineItems:      InvoiceLineItem[],
    Subtotal:       decimal,
    Tax:            decimal,
    Total:          decimal required check Total = Subtotal + Tax,
    Currency:       enum('USD', 'EUR', 'GBP', 'PLN')
}

ai InvoiceLineItem {
    Description: string required max(200),
    Quantity:    int range(1, 10000),
    UnitPrice:   decimal range(0, 1000000),
    Amount:      decimal check Amount = Quantity * UnitPrice
}
```

### 8.2 Inline Anonymous Schemas

For one-off nested structures (consistent with binary schema syntax):

```sql
ai Receipt {
    StoreName: string,
    Items: {
        Name:  string,
        Price: decimal
    }[],
    Total: decimal
}
```

**Semantics:**
- Anonymous schemas are defined inline
- Not referenceable by name
- Useful for simple nested structures that won't be reused

### 8.3 Composition Depth Limit

AI schemas are limited to 4 levels of nesting. This is a practical limit — deeply nested schemas produce excessively complex prompts that degrade extraction quality.

```sql
-- Level 1: Invoice
-- Level 2: Vendor, InvoiceLineItem
-- Level 3: Address (within Vendor)
-- Level 4: Maximum depth reached
```

Exceeding the depth limit is a compile-time error (AIE012).

### 8.4 Cross-Schema-Type Composition

AI schemas can be used alongside binary and text schemas in the same query:

```sql
binary PdfHeader {
    Magic: byte[4] check Magic[0] = 0x25   -- %PDF
}

text MetadataLine {
    Key:   until ':',
    _:     literal ': ',
    Value: rest trim
}

ai DocumentClassification {
    --- Type of document based on content analysis
    DocumentType: enum('invoice', 'contract', 'letter', 'report', 'other'),
    --- Primary language of the document
    Language:     enum('en', 'de', 'fr', 'pl', 'es'),
    --- Extraction confidence
    _confidence:  decimal
}

SELECT 
    f.Name,
    cl.DocumentType,
    cl.Language
FROM os.files('./documents', true) f
CROSS APPLY Interpret(f.GetBytes(), PdfHeader) header
CROSS APPLY Infer(f.GetFileContent(), DocumentClassification) cl
WHERE cl.DocumentType = 'invoice'
```

---

## 9. Error Handling

### 9.1 Error Code Range

AI schema errors use the `AIE` prefix to distinguish from binary/text errors (`ISE`). Errors are classified by ownership — whether they originate from compiled code (deterministic) or from the runtime (environment-dependent):

| Code | Category | Owner | Description |
|------|----------|-------|-------------|
| AIE001 | Provider | Runtime | Provider not configured or unreachable |
| AIE002 | Provider | Runtime | Model not available on provider |
| AIE003 | Response | Runtime | Provider returned non-JSON response |
| AIE004 | Parsing | Either¹ | JSON response does not match expected schema structure |
| AIE005 | Validation | Compiled | Enum constraint violated |
| AIE006 | Validation | Compiled | Range constraint violated |
| AIE007 | Validation | Compiled | Check constraint violated |
| AIE008 | Validation | Compiled | Pattern constraint violated |
| AIE009 | Validation | Compiled | Required field is null or empty |
| AIE010 | Validation | Compiled | String length constraint violated |
| AIE011 | Exhaustion | Runtime | All retry attempts exhausted (if runtime implements retry) |
| AIE012 | Schema | Compiled | Composition depth limit exceeded |
| AIE013 | Schema | Compiled | Circular schema reference |
| AIE014 | Timeout | Runtime | Inference call timed out |
| AIE015 | Capability | Runtime | Required capability not available (e.g., vision) |
| AIE016 | Content | Either¹ | Input content is empty or unreadable |

¹ "Either" means the error may be raised by the compiled deserializer or by the runtime depending on when the issue is detected.

**Compiled errors** (AIE004–AIE010, AIE012–AIE013) have deterministic behavior — the generated code always raises them for the same invalid input. **Runtime errors** (AIE001–AIE003, AIE011, AIE014–AIE016) depend on the host environment and may vary between runs.

### 9.2 Error Structure

```
AIE007: Check constraint failed for field 'Amount'
  Schema: InvoiceLineItem
  Constraint: Amount = Quantity * UnitPrice
  Extracted values: Amount=150.00, Quantity=10, UnitPrice=14.00
  Expected: 140.00
```

### 9.3 Error Behavior by Function

| Function | On Failure |
|----------|-----------|
| `Infer` | Throws `AiInferenceException` |
| `TryInfer` | Returns `null` |

Whether the runtime retries before reporting failure is entirely the runtime's decision. The spec provides tools for retry (see Section 11) but does not mandate any retry behavior.

### 9.4 Partial Extraction (Debugging)

For debugging extraction issues:

```sql
SELECT 
    p.ExtractedFields,
    p.ValidationErrors,
    p.RawResponse
FROM os.file('./problematic-invoice.pdf') f
CROSS APPLY PartialInfer(f.Base64File(), Invoice) p
```

`PartialInfer` returns:
- `ExtractedFields`: Dictionary of successfully extracted and validated fields
- `ValidationErrors`: List of validation errors with field names and details
- `RawResponse`: The raw JSON response before parsing

---

## 10. Runtime Contract

### 10.1 Compilation Artifacts

Each AI schema compiles to a set of artifacts that the engine uses during query execution:

- **JSON Schema** — a structured output constraint document derived deterministically from field names, types, and modifiers. Providers that support structured output mode can use this directly; others receive it as prompt text.
- **Prompt template** — a natural-language extraction instruction generated from doc comments, field names, types, and constraints. Code comments (`--`) are excluded. The same schema always produces the same prompt.
- **Validator** — a compiled function that checks an extracted instance against all declared constraints (`required`, `range`, `min`, `max`, `pattern`, `check`, `enum`). Validation is a pure function: same input always produces the same errors.
- **Retry prompt generator** — a function that produces a corrective prompt from validation errors and the previous response. This is a tool provided to the runtime; the specification does not mandate retry behavior.
- **Deserializer** — a function that maps JSON to the typed output object.

These artifacts are the output of the compilation phase. Query authors never interact with them directly — the engine passes them to the host runtime through the inference contract described below.

### 10.2 Host Runtime Contract

The engine delegates all LLM communication to the host program through a single inference entry point. The host is responsible for:

- LLM provider selection and communication
- Authentication and credential management
- Retry policy (using the compiled validator and retry prompt generator)
- Rate limiting, timeout management, and backoff
- Caching, parallelism, and batching (if desired)
- Logging and telemetry (if desired)

**What the engine provides to the host per inference call:**

| Artifact | Description |
|----------|-------------|
| Content | The unstructured input to extract from (text or base64 image) |
| Prompt template | Generated extraction instructions (does NOT include the content itself) |
| JSON Schema | Structured output constraint document |
| Schema name | The schema identifier (e.g., "Invoice", "Receipt") |
| Annotations | Key-value pairs from `[key: 'value']` syntax on the schema declaration |
| Hint | Optional extraction hint from the query author (null if none) |
| Deserializer | Converts JSON response to the typed output object |
| Validator | Checks extracted instance against compiled constraints |
| Retry prompt generator | Produces corrective prompt from validation errors |

**What the host returns:** either a successful typed result or a failure with error details.

### 10.3 Model Selection Patterns

The specification does not prescribe how the runtime selects models. Schema annotations (§4.2) provide a transport mechanism for query-author intent. Hosts combine annotations with their own configuration to make routing decisions.

**Annotation-based routing (recommended):**

Query authors express intent via annotations; the runtime maps intent to models:

```sql
ai Receipt [tier: 'economy', fallback: 'true'] { ... }
ai SecurityAudit [tier: 'premium'] { ... }
```

The host reads `tier`, `fallback`, or any custom annotation keys from the request's annotations dictionary and routes accordingly.

**Runtime configuration (simplest):**

The host configures a single default model. All inference calls use it. No annotations needed.

**Schema-name routing:**

The host inspects the schema name and routes to different models (e.g., "Receipt" → one model, "LogClassification" → another).

**Content-based routing:**

The host detects whether content is base64 (image) or plain text and selects a vision-capable model accordingly. May combine with a `vision` annotation.

The spec takes no position on which approach is correct. All work within the inference contract boundary. Approaches can be combined.

### 10.4 Specification Guarantees

The spec guarantees deterministic behavior for everything it controls:

| Aspect | Guarantee |
|--------|-----------|
| Schema compilation | Same schema always produces the same JSON Schema, prompt template, and validation logic |
| Validation | The validator is a pure function — same input always produces the same errors |
| Deserialization | The deserializer deterministically maps JSON to typed objects |
| Retry prompt generation | The retry prompt generator deterministically produces corrective prompts from errors |
| Conditional field evaluation | `when` clause conditions are evaluated by compiled code |
| Type mapping | AI types map to runtime types per Appendix B |

### 10.5 Implementation Guidance

For implementors of the host inference runner, the spec recommends (but does not require):

1. **Always validate after deserialization.** The validation encodes constraints that the LLM's JSON schema mode may not fully enforce (e.g., cross-field `check` expressions).

2. **Use the retry prompt generator for retries** rather than constructing custom retry prompts. The generated prompt includes specific constraint violations that improve correction rates.

3. **Treat the JSON Schema as a structured output constraint** for providers that support it (e.g., JSON mode, tool use). For providers that don't, include the schema as text in the prompt.

4. **Handle hints by appending to the prompt**, not replacing it. The hint provides additional context; the prompt template provides the extraction structure.

5. **Read annotations for routing decisions.** Ignore annotations you don't recognize — this allows query authors to add annotations for future runtime features without breaking current behavior.

---

## 11. Grammar Specification

### 11.1 AI Schema Definition

```ebnf
ai_schema       ::= 'ai' identifier annotations? '{' schema_body '}'

annotations     ::= '[' annotation_list ']'

annotation_list ::= annotation (',' annotation)*

annotation      ::= identifier ':' string_literal

schema_body     ::= schema_doc_comment? ai_field_list

schema_doc_comment ::= doc_comment+

ai_field_list   ::= ai_field (',' ai_field)* ','?

ai_field        ::= (doc_comment | code_comment | block_comment)* 
                     identifier ':' ai_type ai_modifiers? 
                     when_clause? check_clause?

ai_type         ::= primitive_type
                   | enum_type
                   | array_type
                   | schema_reference
                   | inline_schema

primitive_type  ::= 'string' | 'int' | 'long' | 'decimal' | 'float' | 'double' 
                   | 'bool' | 'datetime' | 'date' | 'time'

enum_type       ::= 'enum' '(' enum_value_list ')'

enum_value_list ::= (doc_comment | code_comment)* string_literal 
                     (',' (doc_comment | code_comment)* string_literal)+

array_type      ::= ai_type '[]'

schema_reference ::= identifier

inline_schema   ::= '{' ai_field_list '}'
```

### 11.2 Comment Grammar

```ebnf
doc_comment     ::= '---' ' ' (any character except newline)* newline

code_comment    ::= '--' (any character except newline)* newline

block_comment   ::= '/*' (any character)* '*/'
```

### 11.3 Modifier Grammar

```ebnf
ai_modifiers    ::= ai_modifier+

ai_modifier     ::= 'required'
                   | 'max' '(' integer_literal ')'
                   | 'min' '(' integer_literal ')'
                   | 'range' '(' numeric_literal ',' numeric_literal ')'
                   | 'pattern' string_literal

when_clause     ::= 'when' expression

check_clause    ::= 'check' expression
```

### 11.4 Inference Function Grammar

```ebnf
infer_call      ::= 'Infer' '(' expression ',' schema_reference (',' expression)? ')'
                   | 'TryInfer' '(' expression ',' schema_reference (',' expression)? ')'
                   | 'PartialInfer' '(' expression ',' schema_reference ')'
```

---

## 12. Examples

### 12.1 Invoice Processing (Phase 1 Primary Use Case)

```sql
ai Vendor {
    --- Company or individual that issued the invoice
    Name:    string required,
    --- Tax identification number (VAT ID, EIN, NIP, etc.)
    TaxId:   string,
    --- Full postal address
    Address: string max(300)
}

ai InvoiceLineItem {
    --- Product or service description
    Description: string required max(200),
    --- Number of units purchased
    Quantity:    int range(1, 99999),
    --- Price per single unit
    UnitPrice:   decimal range(0, 9999999),
    --- Line total (should equal Quantity × UnitPrice)
    Amount:      decimal check Amount = Quantity * UnitPrice
}

ai Invoice {
    --- Extract invoice data from a scanned or digital invoice document.
    --- The invoice may be in any language and any layout.

    Vendor:         Vendor,
    --- Unique invoice identifier
    InvoiceNumber:  string required pattern '[A-Za-z0-9/-]+',
    --- Date the invoice was issued
    InvoiceDate:    datetime required,
    --- Date payment is due
    DueDate:        datetime check DueDate >= InvoiceDate,
    --- Individual line items
    LineItems:      InvoiceLineItem[],
    --- Amount before tax
    Subtotal:       decimal,
    --- Tax rate as percentage
    TaxRate:        decimal range(0, 100),
    --- Total tax amount
    TaxAmount:      decimal,
    --- Total amount due
    Total:          decimal required check Total = Subtotal + TaxAmount,
    --- ISO 4217 currency code
    Currency:       enum('USD', 'EUR', 'GBP', 'PLN', 'CHF', 'CZK'),
    --- Payment terms if specified
    PaymentTerms:   string max(200),
    --- Additional notes or comments on the invoice
    Notes:          string max(500)
}

-- Process all invoices in a directory
SELECT 
    f.Name as FileName,
    inv.Vendor.Name as VendorName,
    inv.InvoiceNumber,
    inv.InvoiceDate,
    inv.Total,
    inv.Currency
FROM os.files('./invoices', true) f
CROSS APPLY Infer(f.Base64File(), Invoice) inv
WHERE f.Extension IN ('.pdf', '.jpg', '.png')
ORDER BY inv.InvoiceDate DESC
```

**With line item expansion:**

```sql
SELECT 
    inv.Vendor.Name as Vendor,
    inv.InvoiceNumber,
    li.Description,
    li.Quantity,
    li.UnitPrice,
    li.Amount
FROM os.files('./invoices', true) f
CROSS APPLY Infer(f.Base64File(), Invoice) inv
CROSS APPLY inv.LineItems li
WHERE li.Amount > 100
ORDER BY li.Amount DESC
```

**Cross-referencing invoices with bank statements:**

```sql
ai Invoice { /* as above */ }

table BankTransaction {
    Date string,
    Description string,
    Amount string,
    Balance string
};
couple separatedvalues.comma with table BankTransaction as Transactions;

with Invoices as (
    SELECT 
        inv.Vendor.Name as VendorName,
        inv.Total,
        inv.InvoiceDate,
        inv.InvoiceNumber
    FROM os.files('./invoices', true) f
    CROSS APPLY Infer(f.Base64File(), Invoice) inv
),
BankData as (
    SELECT 
        ToDecimal(Amount) as Amount,
        Description,
        ToDateTime(Date, 'yyyy-MM-dd') as TransactionDate
    FROM Transactions('./bank-statement.csv', true, 0)
)
SELECT
    i.VendorName,
    i.InvoiceNumber,
    i.Total as InvoiceAmount,
    b.Amount as PaidAmount,
    b.TransactionDate as PaymentDate
FROM Invoices i
INNER JOIN BankData b ON Abs(i.Total - Abs(b.Amount)) < 0.01
ORDER BY i.InvoiceDate
```

### 12.2 Receipt Processing with Hint

```sql
ai ReceiptItem {
    --- Item name as printed on receipt
    Name:      string required,
    --- Quantity purchased
    Quantity:  int range(1, 100),
    --- Item price
    Price:     decimal range(0, 10000)
}

ai Receipt {
    --- Extract data from a photographed paper receipt.
    --- The receipt may be crumpled, at an angle, or partially obscured.

    --- Store or restaurant name
    StoreName:    string required,
    --- Full address if visible
    StoreAddress: string,
    --- Transaction date and time
    Date:         datetime,
    --- Individual items purchased
    Items:        ReceiptItem[],
    --- Amount before tax
    Subtotal:     decimal,
    --- Tax amount
    Tax:          decimal,
    --- Total paid
    Total:        decimal required,
    --- How the customer paid
    PaymentMethod: enum('cash', 'credit_card', 'debit_card', 'mobile_pay', 'other')
}

SELECT 
    f.Name,
    r.StoreName,
    r.Date,
    r.Total,
    r.PaymentMethod
FROM os.files('~/receipts/2025', true) f
CROSS APPLY Infer(f.Base64File(), Receipt) r
WHERE f.Extension IN ('.jpg', '.png')
ORDER BY r.Date DESC
```

### 12.3 Log Analysis (Phase 2)

```sql
ai LogClassification {
    --- Classify log events by type and impact.

    --- Type of event recorded
    EventType:    enum('error', 'warning', 'slow_query', 'timeout', 
                       'auth_failure', 'resource_exhaustion', 'business_event', 'other'),
    --- System component that generated the event
    Component:    string max(100),
    --- Impact on end users
    UserImpact:   enum('none', 'degraded', 'partial_outage', 'full_outage'),
    --- Root cause if determinable from log line alone
    RootCause:    string max(300),
    --- Suggested action
    Action:       enum('ignore', 'monitor', 'investigate', 'page_oncall')
}

text SyslogLine {
    Timestamp: until ' ',
    _:         literal ' ',
    Host:      until ' ',
    _:         literal ' ',
    Process:   until ':',
    _:         literal ': ',
    Message:   rest
}

SELECT 
    log.Timestamp,
    log.Host,
    log.Process,
    cl.EventType,
    cl.Component,
    cl.UserImpact,
    cl.Action
FROM os.file('/var/log/syslog') f
CROSS APPLY Lines(f.GetContent()) line
CROSS APPLY Parse(line.Value, SyslogLine) log
CROSS APPLY Infer(log.Message, LogClassification) cl
WHERE cl.UserImpact <> 'none'
ORDER BY 
    CASE cl.UserImpact 
        WHEN 'full_outage' THEN 1 
        WHEN 'partial_outage' THEN 2 
        WHEN 'degraded' THEN 3 
        ELSE 4 
    END
```

### 12.4 Legacy Data Migration Assessment (Phase 2)

```sql
text CobolRecord {
    AccountId:    chars[10],
    AccountType:  chars[2],
    CustomerName: chars[30] trim,
    Balance:      chars[12],
    StatusCode:   chars[1],
    LastActivity: chars[8],
    Filler:       chars[37]
}

ai BusinessMeaning {
    --- Interpret mainframe account record fields into business terms.

    --- What this account type means in business terms
    AccountCategory:    enum('checking', 'savings', 'loan', 'credit_line', 
                             'investment', 'escrow', 'unknown'),
    --- Whether this record is still active
    IsActive:           bool,
    --- Estimated migration complexity
    MigrationRisk:      enum('trivial', 'low', 'moderate', 'high', 'needs_human'),
    --- Why this risk level was assigned
    RiskReason:         string max(300),
    --- Data quality concern if any
    DataQualityIssue:   string max(200)
}

SELECT 
    r.AccountId,
    r.CustomerName,
    ToDecimal(r.Balance) / 100.0 as Balance,
    bm.AccountCategory,
    bm.IsActive,
    bm.MigrationRisk,
    bm.RiskReason
FROM os.file('/mainframe/ACCTMAST.DAT') f
CROSS APPLY Lines(f.GetContent()) line
CROSS APPLY Parse(line.Value, CobolRecord) r
CROSS APPLY Infer(
    'AccountType=' + r.AccountType + ' StatusCode=' + r.StatusCode + 
    ' LastActivity=' + r.LastActivity + ' Balance=' + r.Balance,
    BusinessMeaning,
    'US bank mainframe record. Account types are 2-char codes. Status: A=active, C=closed, F=frozen, D=dormant.'
) bm
WHERE bm.MigrationRisk IN ('high', 'needs_human')
ORDER BY bm.MigrationRisk, r.AccountId
```

### 12.5 Code Quality Analysis (Phase 3)

```sql
ai CodeSmell {
    --- Identify code quality issues in a C# method body.

    --- Type of code quality issue found
    SmellType:       enum('hardcoded_secret', 'sql_injection', 'null_deref',
                          'resource_leak', 'magic_number', 'dead_code',
                          'god_method', 'copy_paste', 'none'),
    --- Severity of the issue
    Severity:        enum('info', 'warning', 'error', 'critical'),
    --- Approximate line range where the issue occurs
    Location:        string max(50),
    --- Human-readable description
    Description:     string max(300),
    --- Suggested fix
    Recommendation:  string max(300)
}

ai SecurityAssessment {
    --- Assess security posture of a C# method.

    HasHardcodedCredentials: bool,
    HasInjectionVulnerability: bool,
    UsesDeprecatedCrypto: bool,
    ExposesInternalDetails: bool,
    --- Overall risk considering all factors
    OverallRisk: enum('safe', 'low', 'medium', 'high', 'critical')
}

SELECT 
    c.Name as ClassName,
    m.Name as MethodName,
    m.LinesOfCode,
    m.CyclomaticComplexity,
    smell.SmellType,
    smell.Severity,
    smell.Description,
    sec.OverallRisk
FROM csharp.solution('./MyProject.sln') s
CROSS APPLY s.Projects p
CROSS APPLY p.Documents d
CROSS APPLY d.Classes c
CROSS APPLY c.Methods m
CROSS APPLY Infer(m.Body, CodeSmell) smell
CROSS APPLY Infer(m.Body, SecurityAssessment) sec
WHERE smell.SmellType <> 'none' OR sec.OverallRisk IN ('high', 'critical')
ORDER BY 
    CASE sec.OverallRisk 
        WHEN 'critical' THEN 1 WHEN 'high' THEN 2 
        WHEN 'medium' THEN 3 ELSE 4 
    END,
    m.CyclomaticComplexity DESC
```

### 12.6 Multi-Stage Pipeline (Privacy Pattern)

Combining stages where annotations guide the runtime toward appropriate model selection:

```sql
-- Annotation signals: must be local (privacy), needs vision
ai ImageDescription [tier: 'local', vision: 'required'] {
    --- Describe the visual content of a document image.
    --- Focus on layout, text regions, and document structure.

    --- Brief factual description of the image content
    Description:  string required max(500),
    --- Objects or entities visible
    Objects:      string[],
    --- Whether the image contains text
    ContainsText: bool,
    --- Any text visible in the image
    VisibleText:  string max(1000) when ContainsText = true
}

-- Annotation signals: text-only classification, economy tier is fine
ai DocumentClassification [tier: 'economy'] {
    --- Classify a document based on its textual description.

    --- Type of document
    DocumentType:  enum('invoice', 'receipt', 'contract', 'letter', 
                        'form', 'id_document', 'photo', 'other'),
    --- Primary language
    Language:      string max(10),
    --- Whether OCR processing would improve extraction
    RequiresOcr:   bool,
    --- Processing priority
    Priority:      enum('low', 'normal', 'high')
}

-- Stage 1: Extract image descriptions (runtime uses local vision model)
with Descriptions as (
    SELECT 
        f.Name,
        f.FullName,
        desc.Description,
        desc.ContainsText,
        desc.VisibleText
    FROM os.files('./incoming-documents', true) f
    CROSS APPLY Infer(f.Base64File(), ImageDescription) desc
    WHERE f.Extension IN ('.jpg', '.png', '.pdf')
)
-- Stage 2: Classify based on text only (runtime uses cheap cloud model)
SELECT 
    d.Name,
    cl.DocumentType,
    cl.Language,
    cl.Priority,
    d.VisibleText
FROM Descriptions d
CROSS APPLY Infer(
    d.Description + CASE WHEN d.VisibleText IS NOT NULL 
        THEN '\nVisible text: ' + d.VisibleText 
        ELSE '' 
    END,
    DocumentClassification
) cl
WHERE cl.DocumentType IN ('invoice', 'receipt', 'contract')
ORDER BY cl.Priority DESC
```

The privacy benefit: Stage 1 is annotated `tier: 'local'` — the runtime routes it to Ollama or another on-premise model. The actual image never leaves the machine. Stage 2 is annotated `tier: 'economy'` — only the text description (not the image) reaches a cloud model. The query author expresses the privacy constraint; the runtime enforces it.

---

## 13. Future Considerations

### 13.1 Embedding Integration

AI schemas could be extended with embedding-aware fields:

```sql
ai EnrichedCommit {
    Category:    enum('bugfix', 'feature', 'refactor'),
    Embedding:   vector(384)    -- Future: embedding vector type
}
```

This would enable semantic similarity JOINs between AI-extracted data from different sources.

### 13.2 Streaming Extraction

For large documents, extraction could be streamed section-by-section rather than processing the entire document at once.

### 13.3 Schema Evolution

Version annotations for schemas that evolve over time:

```sql
ai Receipt version 2 {
    --- Added in v2: loyalty program tracking
    LoyaltyNumber: string,
    /* ... existing fields ... */
}
```

### 13.4 Feedback Loop

Store human corrections to AI extractions and use them as few-shot examples for future extractions. The runtime could maintain a correction store and inject examples into prompts automatically.

### 13.5 Explanation Mode

A reserved field like `_explanation` could request the LLM's reasoning for each extracted field, useful for auditing in compliance scenarios.

---

## Appendix A: Reserved Keywords

```
ai, check, date, datetime, decimal, double, enum, float, 
int, long, max, min, pattern, range, required, time, when
```

Note: `check` and `when` are shared with binary/text schemas. All other AI-specific keywords (`ai`, `enum`, `range`, `max`, `min`, `required`, `pattern`) are new.

---

## Appendix B: Type Mapping

| AI Type | .NET Type | JSON Schema Type | Default |
|---------|-----------|-----------------|---------|
| `string` | `string?` | `string` | `null` |
| `string required` | `string` | `string` (required) | N/A (must be present) |
| `int` | `int?` | `integer` | `null` |
| `long` | `long?` | `integer` | `null` |
| `decimal` | `decimal?` | `number` | `null` |
| `float` | `float?` | `number` | `null` |
| `double` | `double?` | `number` | `null` |
| `bool` | `bool?` | `boolean` | `null` |
| `datetime` | `DateTime?` | `string` (ISO 8601) | `null` |
| `date` | `DateOnly?` | `string` (ISO 8601) | `null` |
| `time` | `TimeOnly?` | `string` (ISO 8601) | `null` |
| `enum(...)` | `string?` | `string` + `enum` | `null` |
| `T[]` | `T[]` | `array` | `[]` |
| Schema ref | Generated class | `object` | `null` |

---

## Appendix C: Error Code Summary

| Code | Owner | Description |
|------|-------|-------------|
| AIE001 | Runtime | Provider not configured or unreachable |
| AIE002 | Runtime | Model not available on provider |
| AIE003 | Runtime | Provider returned non-JSON response |
| AIE004 | Either | JSON structure does not match expected schema |
| AIE005 | Compiled | Enum constraint violated |
| AIE006 | Compiled | Range constraint violated |
| AIE007 | Compiled | Check constraint violated |
| AIE008 | Compiled | Pattern constraint violated |
| AIE009 | Compiled | Required field is null or empty |
| AIE010 | Compiled | String length constraint violated |
| AIE011 | Runtime | All retry attempts exhausted |
| AIE012 | Compiled | Composition depth limit exceeded |
| AIE013 | Compiled | Circular schema reference |
| AIE014 | Runtime | Inference call timed out |
| AIE015 | Runtime | Required capability not available |
| AIE016 | Either | Input content is empty or unreadable |

---

## Appendix D: Comparison with Existing Approaches

| Feature | TABLE/COUPLE + LlmExtract | Pydantic/Instructor | BAML | AI Schema |
|---------|--------------------------|---------------------|------|-----------|
| Language | SQL + manual prompt | Python | Custom DSL | SQL (native) |
| Validation | Runtime type cast | Pydantic validators | Type system | Compiled validators |
| Conditional fields | No | No | No | Yes (`when` clause) |
| Cross-source JOIN | Yes (Musoq) | No | No | Yes (native) |
| Retry tools | Manual | Built-in | Built-in | Compiled (runtime decides policy) |
| Schema = prompt | No | Partial | Yes | Yes (doc comments only) |
| Model routing metadata | No | No | No | Yes (annotations) |
| Binary/text composition | No | No | No | Yes |
| Runtime agnostic | No | No | No | Yes (inference runner interface) |
| Offline-capable | Yes (Ollama) | No | No | Yes (runtime decides) |
| Zero reflection | N/A | No | No | Yes (compile-time generated) |

---

*End of Specification*