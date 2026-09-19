# Requisitos — Sprint 4 (rubric oficial)

> Transcrição do enunciado *2TDS Fevereiro — Challenge 2026 — Advanced Business Development
> with .NET*. **Não editar** o conteúdo: se o professor esclarecer algo, registre em
> [`decisoes/`](decisoes/) e referencie daqui.

## Objetivo

Entregar o projeto completo e funcional, contemplando todas as funcionalidades das Sprints
1, 2 e 3, integradas e funcionando em conjunto. Foco na consolidação de:

1. Arquitetura e qualidade de código
2. API RESTful completa
3. Persistência de dados relacional e NoSQL
4. Monitoramento, observabilidade e testes
5. Documentação final

## 1. Arquitetura e Código — 30 pts

- Clean Architecture aplicada corretamente, com separação de camadas.
- Princípios SOLID e Clean Code evidentes no código.
- Injeção de Dependência configurada adequadamente.
- Tratamento de exceções global e consistente.

## 2. API RESTful Completa — 20 pts

- Documentação Swagger/OpenAPI atualizada.
- Paginação, ordenação e filtros implementados.
- HATEOAS implementado nos endpoints de consulta.
- Autenticação e Autorização com JWT ou Identity.

## 3. Persistência de Dados — 20 pts

- Entity Framework Core com migrações aplicadas (**Oracle ou SQL Server**).
- MongoDB integrado para operações NoSQL.
- Repositórios concretos implementados com o padrão Repository.

## 4. Monitoramento, Observabilidade e Testes — 20 pts

- Health Checks configurados e funcionais.
- Logging estruturado implementado.
- Testes unitários e de integração com padrão AAA (xUnit).
- Cobertura mínima de testes das camadas de Domínio e Aplicação.

## 5. Documentação e README Final — 10 pts

- README completo: visão geral, arquitetura, endpoints, instalação, testes e integrantes.
- Diagrama de arquitetura da solução.
- Documentação dos endpoints via Swagger exportado ou descrita no README.

## Penalidades

| Penalidade | Motivo |
|---|---|
| −20 pts | Projeto não compilar |
| −10 pts | Ausência de README ou README incompleto |
| −30 pts | Ausência total de testes |
