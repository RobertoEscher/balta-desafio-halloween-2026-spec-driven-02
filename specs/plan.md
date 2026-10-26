# Plano técnico

## Contexto
Implementa a especificação do encurtador de links, respeitando a constituição
do projeto.

## Arquitetura
- API HTTP em Minimal API, um arquivo de endpoints por recurso
- Camada de aplicação com LinkService, dona das regras de negócio RF-01 a RF-07 e RN-01 a RN-06
- Camada de dados com EF Core e um repositório por agregado
- Endpoint valida a forma da entrada, chama o serviço e traduz o resultado em resposta; regras de negócio ficam no serviço

## Decisões
- D-01: SQLite como armazenamento, porque o RF-08 exige persistência
  entre reinícios
- D-02: contador incrementado por UPDATE atômico no banco, e não lido e
  reescrito na aplicação, para atender a RF-05 e RN-04 sob acesso concorrente
- D-03: códigos automáticos e personalizados usam exatamente as letras
  maiúsculas de A a Z e os dígitos de 1 a 9, conforme RF-02, RF-03 e RN-05
- D-04: unicidade do código garantida por índice único no banco, além da
  checagem no serviço, porque a checagem sozinha tem corrida

## Modelo de dados
Link
- Id, Code (único), TargetUrl, CreatedAtUtc, ExpiresAtUtc (nulo), AccessCount,
  com ExpiresAtUtc representando o início do dia UTC seguinte à data de expiração informada

## Contratos
- POST /api/links entrada: targetUrl, code opcional, expiresOnUtc opcional
  (data de calendário em UTC, no formato YYYY-MM-DD)
- POST /api/links saída: shortUrl, code
- GET /{code} redireciona com 302, ou 404 se não existe, ou 410 se expirou
- GET /api/links/{code}/stats devolve o total de acessos sem autenticação, conforme RF-07

## Riscos
- Concorrência na criação de código próprio, mitigada por D-04
- Verificação de colisão em códigos automáticos deve gerar outro código e tentar novamente, conforme RN-06
- Crescimento da tabela de links sem política de limpeza, fora de escopo por ora