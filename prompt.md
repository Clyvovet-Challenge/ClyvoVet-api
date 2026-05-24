# PROMPT PARA A API .NET — CLYVO VET

Cole este prompt inteiro na conversa do Claude Code que está trabalhando na API .NET.

---

## Contexto do Projeto

Estou desenvolvendo o **Clyvo Vet**, um sistema de gestão veterinária com duas APIs independentes compartilhando o mesmo banco Oracle XE hospedado na FIAP:

- **API Java** (Spring Boot) — gerencia: tutor, animal, clínica, veterinário, evento clínico, pagamento  
- **API .NET** (ASP.NET Core 8) — gerencia: produto, sugestão de produto, lembrete, evento pet  
  — e também **lê** as tabelas da API Java (animal, tutor) para montar respostas enriquecidas

Ambas as APIs rodam em **containers Docker separados** em uma VM Linux na Azure. **Não há Docker Compose nem orquestrador.**

---

## Stack Exata da API .NET

- .NET 8.0 / ASP.NET Core 8.0
- EF Core 8.0 → pacote: `Oracle.EntityFrameworkCore 8.21.121`
- Oracle Database XE (acessado remotamente pela connection string)
- Swagger / OpenAPI (Swashbuckle.AspNetCore)

---

## DDL COMPLETO — Todas as Tabelas do Banco

> O banco já existe e está em produção. A API .NET deve usar **Database-First** ou **Fluent API sem migrations automáticas**. Não rode `dotnet ef migrations add` contra este banco.

```sql
-- ============================================================
-- FUNÇÃO uuid (gerada por trigger antes de todo INSERT)
-- ============================================================
CREATE OR REPLACE FUNCTION fn_uuid RETURN VARCHAR2 IS
BEGIN
    RETURN LOWER(REGEXP_REPLACE(
        RAWTOHEX(SYS_GUID()),
        '([A-F0-9]{8})([A-F0-9]{4})([A-F0-9]{4})([A-F0-9]{4})([A-F0-9]{12})',
        '\1-\2-\3-\4-\5'
    ));
END fn_uuid;
/

-- ============================================================
-- SEQUENCE (somente para log_erros — ID numérico)
-- ============================================================
CREATE SEQUENCE seq_log_erros START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE;

-- ============================================================
-- LOG DE ERROS (tabela de sistema — não expor via API)
-- ============================================================
CREATE TABLE t_clyvo_log_erros (
    id              NUMBER        DEFAULT seq_log_erros.NEXTVAL PRIMARY KEY,
    nome_procedure  VARCHAR2(100) NOT NULL,
    usuario         VARCHAR2(100) DEFAULT USER,
    data_erro       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    codigo_erro     NUMBER,
    mensagem_erro   VARCHAR2(4000)
);

-- ============================================================
-- 1. TUTOR  (domínio Java — .NET só lê)
-- ============================================================
CREATE TABLE t_clyvo_tutor (
    id              VARCHAR2(36)  NOT NULL,
    cpf             VARCHAR2(11),
    nome            VARCHAR2(150) NOT NULL,
    data_nascimento DATE,
    genero          VARCHAR2(10),
    email           VARCHAR2(200),
    telefone        VARCHAR2(20),
    rua             VARCHAR2(300),
    numero          VARCHAR2(10),
    bairro          VARCHAR2(150),
    cidade          VARCHAR2(100),
    estado          VARCHAR2(50),
    cep             VARCHAR2(10),
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_tutor         PRIMARY KEY (id),
    CONSTRAINT uk_tutor_cpf     UNIQUE (cpf),
    CONSTRAINT uk_tutor_email   UNIQUE (email),
    CONSTRAINT chk_tutor_genero CHECK (genero IN ('MASCULINO','FEMININO','OUTRO'))
);
CREATE OR REPLACE TRIGGER trg_tutor_id
BEFORE INSERT ON t_clyvo_tutor FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 2. ANIMAL  (domínio Java — .NET lê e usa animal_id como FK)
-- ============================================================
CREATE TABLE t_clyvo_animal (
    id              VARCHAR2(36)  NOT NULL,
    nome            VARCHAR2(100) NOT NULL,
    raca            VARCHAR2(100),
    especie         VARCHAR2(50),
    porte           VARCHAR2(10),
    cor             VARCHAR2(80),
    genero          VARCHAR2(10),
    data_nascimento DATE,
    observacoes     VARCHAR2(1000),
    peso            NUMBER(5,2),
    castrado        NUMBER(1)     DEFAULT 0,
    microchip       VARCHAR2(50),
    foto_url        VARCHAR2(500),
    qr_code         VARCHAR2(100),
    tutor_id        VARCHAR2(36),
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_animal             PRIMARY KEY (id),
    CONSTRAINT fk_animal_tutor       FOREIGN KEY (tutor_id) REFERENCES t_clyvo_tutor(id),
    CONSTRAINT chk_animal_castrado   CHECK (castrado IN (0,1)),
    CONSTRAINT uk_animal_microchip   UNIQUE (microchip),
    CONSTRAINT uk_animal_qr_code     UNIQUE (qr_code),
    CONSTRAINT chk_animal_porte      CHECK (porte   IN ('PEQUENO','MEDIO','GRANDE')),
    CONSTRAINT chk_animal_genero     CHECK (genero  IN ('MACHO','FEMEA','DESCONHECIDO'))
);
CREATE OR REPLACE TRIGGER trg_animal_id
BEFORE INSERT ON t_clyvo_animal FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 3. CLINICA  (domínio Java — .NET só lê)
-- ============================================================
CREATE TABLE t_clyvo_clinica (
    id        VARCHAR2(36)  NOT NULL,
    nome      VARCHAR2(200) NOT NULL,
    cnpj      VARCHAR2(14),
    telefone  VARCHAR2(20),
    email     VARCHAR2(200),
    rua       VARCHAR2(300),
    numero    VARCHAR2(10),
    bairro    VARCHAR2(150),
    cidade    VARCHAR2(100),
    estado    VARCHAR2(50),
    cep       VARCHAR2(10),
    criado_em TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_clinica      PRIMARY KEY (id),
    CONSTRAINT uk_clinica_cnpj UNIQUE (cnpj)
);
CREATE OR REPLACE TRIGGER trg_clinica_id
BEFORE INSERT ON t_clyvo_clinica FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 4. VETERINARIO  (domínio Java — .NET só lê)
-- ============================================================
CREATE TABLE t_clyvo_veterinario (
    id              VARCHAR2(36)  NOT NULL,
    cpf             VARCHAR2(11),
    nome            VARCHAR2(150) NOT NULL,
    data_nascimento DATE,
    genero          VARCHAR2(10),
    email           VARCHAR2(200),
    telefone        VARCHAR2(20),
    especialidade   VARCHAR2(100),
    crmv            VARCHAR2(30),
    rua             VARCHAR2(300),
    numero          VARCHAR2(10),
    bairro          VARCHAR2(150),
    cidade          VARCHAR2(100),
    estado          VARCHAR2(50),
    cep             VARCHAR2(10),
    clinica_id      VARCHAR2(36),
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_veterinario         PRIMARY KEY (id),
    CONSTRAINT fk_veterinario_clinica FOREIGN KEY (clinica_id) REFERENCES t_clyvo_clinica(id),
    CONSTRAINT uk_vet_cpf             UNIQUE (cpf),
    CONSTRAINT uk_vet_crmv            UNIQUE (crmv),
    CONSTRAINT chk_vet_genero         CHECK (genero IN ('MASCULINO','FEMININO','OUTRO'))
);
CREATE OR REPLACE TRIGGER trg_veterinario_id
BEFORE INSERT ON t_clyvo_veterinario FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 5. EVENTO_CLINICO  (domínio Java — .NET só lê)
-- ============================================================
CREATE TABLE t_clyvo_evento_clinico (
    id              VARCHAR2(36)  NOT NULL,
    data_evento     DATE,
    hora_evento     VARCHAR2(5),
    descricao       VARCHAR2(1000),
    tipo_evento     VARCHAR2(20),
    veterinario_id  VARCHAR2(36),
    animal_id       VARCHAR2(36),
    clinica_id      VARCHAR2(36),
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_evento_clinico     PRIMARY KEY (id),
    CONSTRAINT fk_evento_veterinario FOREIGN KEY (veterinario_id) REFERENCES t_clyvo_veterinario(id),
    CONSTRAINT fk_evento_animal      FOREIGN KEY (animal_id)      REFERENCES t_clyvo_animal(id),
    CONSTRAINT fk_evento_clinica     FOREIGN KEY (clinica_id)     REFERENCES t_clyvo_clinica(id),
    CONSTRAINT chk_evento_tipo       CHECK (tipo_evento IN ('CONSULTA','RETORNO','VACINA','EXAME','CIRURGIA','OUTRO'))
);
CREATE OR REPLACE TRIGGER trg_evento_clinico_id
BEFORE INSERT ON t_clyvo_evento_clinico FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 6. PAGAMENTO  (domínio Java — .NET só lê)
-- ============================================================
CREATE TABLE t_clyvo_pagamento (
    id                VARCHAR2(36)  NOT NULL,
    metodo_pagamento  VARCHAR2(10),
    valor             NUMBER(10,2),
    data_pagamento    DATE,
    descricao         VARCHAR2(500),
    notas             VARCHAR2(1000),
    status_pagamento  VARCHAR2(15),
    evento_id         VARCHAR2(36),
    criado_em         TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_pagamento          PRIMARY KEY (id),
    CONSTRAINT fk_pagamento_evento   FOREIGN KEY (evento_id) REFERENCES t_clyvo_evento_clinico(id),
    CONSTRAINT chk_pagamento_metodo  CHECK (metodo_pagamento IN ('PIX','CARTAO','DINHEIRO','BOLETO')),
    CONSTRAINT chk_pagamento_status  CHECK (status_pagamento IN ('PENDENTE','PAGO','CANCELADO','REEMBOLSADO')),
    CONSTRAINT chk_pagamento_valor   CHECK (valor > 0)
);
CREATE OR REPLACE TRIGGER trg_pagamento_id
BEFORE INSERT ON t_clyvo_pagamento FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 7. PRODUTO  ★ DOMÍNIO .NET — CRUD COMPLETO
-- ============================================================
CREATE TABLE t_clyvo_produto (
    id               VARCHAR2(36)  NOT NULL,
    nome             VARCHAR2(200) NOT NULL,
    descricao        VARCHAR2(1000),
    categoria        VARCHAR2(20),
    preco            NUMBER(10,2),
    especie_indicada VARCHAR2(20),
    ativo            NUMBER(1)     DEFAULT 1,
    criado_em        TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_produto             PRIMARY KEY (id),
    CONSTRAINT chk_produto_categoria  CHECK (categoria        IN ('RACAO','MEDICAMENTO','ACESSORIO','SERVICO','OUTRO')),
    CONSTRAINT chk_produto_especie    CHECK (especie_indicada IN ('CACHORRO','GATO','PASSARO','REPTIL','ROEDOR','TODOS','OUTRO')),
    CONSTRAINT chk_produto_ativo      CHECK (ativo            IN (0,1))
);
CREATE OR REPLACE TRIGGER trg_produto_id
BEFORE INSERT ON t_clyvo_produto FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 8. SUGESTAO_PRODUTO  ★ DOMÍNIO .NET — CRUD COMPLETO
-- animal_id vem do front-end; FK valida existência no banco
-- ============================================================
CREATE TABLE t_clyvo_sugestao_produto (
    id              VARCHAR2(36)  NOT NULL,
    animal_id       VARCHAR2(36)  NOT NULL,
    produto_id      VARCHAR2(36)  NOT NULL,
    justificativa   VARCHAR2(500),
    data_sugestao   DATE          DEFAULT SYSDATE,
    ativo           NUMBER(1)     DEFAULT 1,
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_sugestao_produto  PRIMARY KEY (id),
    CONSTRAINT fk_sugestao_animal   FOREIGN KEY (animal_id)  REFERENCES t_clyvo_animal(id),
    CONSTRAINT fk_sugestao_produto  FOREIGN KEY (produto_id) REFERENCES t_clyvo_produto(id),
    CONSTRAINT chk_sugestao_ativo   CHECK (ativo IN (0,1))
);
CREATE OR REPLACE TRIGGER trg_sugestao_produto_id
BEFORE INSERT ON t_clyvo_sugestao_produto FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 9. LEMBRETE  ★ DOMÍNIO .NET — CRUD COMPLETO
-- ============================================================
CREATE TABLE t_clyvo_lembrete (
    id          VARCHAR2(36)  NOT NULL,
    animal_id   VARCHAR2(36)  NOT NULL,
    titulo      VARCHAR2(200) NOT NULL,
    descricao   VARCHAR2(1000),
    tipo        VARCHAR2(20),
    agendado_em TIMESTAMP     NOT NULL,
    recorrente  NUMBER(1)     DEFAULT 0,
    status      VARCHAR2(20)  DEFAULT 'PENDENTE',
    criado_em   TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_lembrete             PRIMARY KEY (id),
    CONSTRAINT fk_lembrete_animal      FOREIGN KEY (animal_id) REFERENCES t_clyvo_animal(id),
    CONSTRAINT chk_lembrete_tipo       CHECK (tipo       IN ('VACINA','MEDICAMENTO','CONSULTA','HIGIENE','OUTRO')),
    CONSTRAINT chk_lembrete_recorrente CHECK (recorrente IN (0,1)),
    CONSTRAINT chk_lembrete_status     CHECK (status     IN ('PENDENTE','ENVIADO','CANCELADO'))
);
CREATE OR REPLACE TRIGGER trg_lembrete_id
BEFORE INSERT ON t_clyvo_lembrete FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/

-- ============================================================
-- 10. EVENTO_PET  ★ DOMÍNIO .NET — CRUD COMPLETO
-- ============================================================
CREATE TABLE t_clyvo_evento_pet (
    id              VARCHAR2(36)  NOT NULL,
    titulo          VARCHAR2(200) NOT NULL,
    descricao       VARCHAR2(1000),
    tipo            VARCHAR2(20),
    rua             VARCHAR2(300),
    numero          VARCHAR2(10),
    bairro          VARCHAR2(150),
    cidade          VARCHAR2(100),
    estado          VARCHAR2(50),
    cep             VARCHAR2(10),
    data_inicio     DATE          NOT NULL,
    data_fim        DATE,
    especie_alvo    VARCHAR2(20)  DEFAULT 'TODOS',
    organizador     VARCHAR2(200),
    gratuito        NUMBER(1)     DEFAULT 1,
    link_inscricao  VARCHAR2(500),
    ativo           NUMBER(1)     DEFAULT 1,
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_evento_pet               PRIMARY KEY (id),
    CONSTRAINT chk_evento_pet_tipo         CHECK (tipo         IN ('VACINACAO','FEIRA','CASTRACAO','WORKSHOP','OUTRO')),
    CONSTRAINT chk_evento_pet_especie_alvo CHECK (especie_alvo IN ('CACHORRO','GATO','PASSARO','REPTIL','ROEDOR','TODOS','OUTRO')),
    CONSTRAINT chk_evento_pet_gratuito     CHECK (gratuito     IN (0,1)),
    CONSTRAINT chk_evento_pet_ativo        CHECK (ativo        IN (0,1))
);
CREATE OR REPLACE TRIGGER trg_evento_pet_id
BEFORE INSERT ON t_clyvo_evento_pet FOR EACH ROW
BEGIN IF :NEW.id IS NULL THEN :NEW.id := fn_uuid(); END IF; END;
/
```

---

## Regras Críticas para o EF Core com Oracle

### 1. PKs são `string`, NUNCA `Guid`
```csharp
// ✅ CORRETO
public string Id { get; set; } = string.Empty;

// ❌ ERRADO — Oracle não tem tipo UUID nativo
public Guid Id { get; set; }
```

### 2. UUID gerado no banco, não no C#
O banco tem um trigger `BEFORE INSERT` que chama `fn_uuid()` quando `id IS NULL`.  
Configure no EF Core para **não gerar no cliente**:
```csharp
entity.Property(e => e.Id)
    .HasColumnName("ID")
    .HasColumnType("VARCHAR2(36)")
    .ValueGeneratedOnAdd(); // trigger do banco gera
```

### 3. Boolean → `int` (Oracle não tem bool)
```csharp
// Na entidade:
public int Ativo { get; set; } = 1;
public int Recorrente { get; set; } = 0;
public int Gratuito { get; set; } = 1;

// No DbContext (Fluent API):
entity.Property(e => e.Ativo).HasColumnType("NUMBER(1)");
```

### 4. Nomes de tabela e coluna em MAIÚSCULO no Oracle
```csharp
entity.ToTable("T_CLYVO_PRODUTO");

entity.Property(e => e.EspecieIndicada)
    .HasColumnName("ESPECIE_INDICADA")
    .HasColumnType("VARCHAR2(20)");
```

### 5. Timestamps
```csharp
public DateTime CriadoEm { get; set; }

// Fluent API:
entity.Property(e => e.CriadoEm)
    .HasColumnName("CRIADO_EM")
    .HasColumnType("TIMESTAMP")
    .ValueGeneratedOnAdd(); // DEFAULT SYSTIMESTAMP do banco
```

---

## Connection String (Oracle XE — FIAP)

```json
// appsettings.json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=<SEU_USUARIO>;Password=<SUA_SENHA>;Data Source=<HOST_FIAP>:<PORTA>/XEPDB1;"
  }
}
```

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));
```

---

## Estrutura de Pastas Esperada

```
ClyvoVet.Api/
├── Controllers/
│   ├── ProdutoController.cs
│   ├── SugestaoProdutoController.cs
│   ├── LembreteController.cs
│   └── EventoPetController.cs
├── Models/
│   ├── Produto.cs
│   ├── SugestaoProduto.cs
│   ├── Lembrete.cs
│   ├── EventoPet.cs
│   ├── Animal.cs          ← leitura da tabela Java
│   └── Tutor.cs           ← leitura da tabela Java
├── Data/
│   └── AppDbContext.cs
├── DTOs/
│   ├── ProdutoDto.cs
│   ├── SugestaoProdutoDto.cs
│   ├── LembreteDto.cs
│   └── EventoPetDto.cs
├── schema/
│   └── 01_DDL_TABLES.sql  ← copiar o DDL completo acima aqui
└── Program.cs
```

> **A pasta `schema/` é obrigatória para o professor testar a API.**  
> Ela deve conter o DDL completo que, ao ser executado, recria todo o banco.

---

## Tabelas por Domínio

| Tabela | Domínio | Operações .NET |
|--------|---------|----------------|
| `t_clyvo_produto` | **.NET** | GET, POST, PUT, DELETE |
| `t_clyvo_sugestao_produto` | **.NET** | GET, POST, PUT, DELETE |
| `t_clyvo_lembrete` | **.NET** | GET, POST, PUT, DELETE |
| `t_clyvo_evento_pet` | **.NET** | GET, POST, PUT, DELETE |
| `t_clyvo_animal` | Java (leitura) | GET by ID (para validar FK) |
| `t_clyvo_tutor` | Java (leitura) | GET by ID (opcional, para enriquecer resposta) |
| `t_clyvo_clinica` | Java (leitura) | GET (opcional) |
| `t_clyvo_veterinario` | Java (leitura) | GET (opcional) |
| `t_clyvo_evento_clinico` | Java (leitura) | GET (opcional) |
| `t_clyvo_pagamento` | Java (leitura) | GET (opcional) |
| `t_clyvo_log_erros` | Sistema | Não expor via endpoint |

---

## Valores Válidos por Coluna (CHECK constraints)

```
Produto.categoria:        RACAO | MEDICAMENTO | ACESSORIO | SERVICO | OUTRO
Produto.especie_indicada: CACHORRO | GATO | PASSARO | REPTIL | ROEDOR | TODOS | OUTRO

Lembrete.tipo:   VACINA | MEDICAMENTO | CONSULTA | HIGIENE | OUTRO
Lembrete.status: PENDENTE | ENVIADO | CANCELADO

EventoPet.tipo:         VACINACAO | FEIRA | CASTRACAO | WORKSHOP | OUTRO
EventoPet.especie_alvo: CACHORRO | GATO | PASSARO | REPTIL | ROEDOR | TODOS | OUTRO

Animal.especie: CACHORRO | GATO | PASSARO | REPTIL | ROEDOR | OUTRO
Animal.porte:   PEQUENO | MEDIO | GRANDE
Animal.genero:  MACHO | FEMEA | DESCONHECIDO

Tutor.genero:       MASCULINO | FEMININO | OUTRO
Pagamento.metodo:   PIX | CARTAO | DINHEIRO | BOLETO
Pagamento.status:   PENDENTE | PAGO | CANCELADO | REEMBOLSADO
EventoClinico.tipo: CONSULTA | RETORNO | VACINA | EXAME | CIRURGIA | OUTRO
```

---

## Tarefas Prioritárias

1. **Criar a pasta `schema/`** com o arquivo `01_DDL_TABLES.sql` contendo o DDL completo acima
2. **Criar as entidades** para as 4 tabelas do domínio .NET + entidades somente-leitura para Animal e Tutor
3. **Configurar o `AppDbContext`** com Fluent API (nomes de tabela em maiúsculo, tipos Oracle corretos)
4. **Criar os controllers** com CRUD completo para as 4 tabelas do domínio .NET
5. **Configurar Swagger** para o professor conseguir testar os endpoints sem precisar de um cliente HTTP externo
6. **Validar** que ao fazer POST de Produto/Lembrete/EventoPet, o `id` retornado é um UUID válido gerado pelo Oracle (ex: `"3f2a1b4c-5d6e-7f8a-9b0c-1d2e3f4a5b6c"`)
