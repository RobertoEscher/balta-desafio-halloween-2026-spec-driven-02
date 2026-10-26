# Tarefas

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-01 | Criar projeto e estrutura de pastas | Constituição (Stack e Arquitetura) | | Projeto compila |
| T-02 | Modelar a entidade Link e a migração | RF-01, RF-08 | T-01 | Migração aplica no banco |
| T-03 | Gerar código aleatório de 7 caracteres | RF-02, D-03 | T-01 | Teste unitário cobre formato e alfabeto |
| T-04 | Validar URL de destino | RN-02, borda 2048 | T-01 | Teste cobre http, https, URL inválida, maior que 2048 caracteres e destino inacessível |
| T-05 | Criar link com código automático | RF-01, RN-06 | T-02, T-03, T-04 | Teste de integração cria e retorna 201; teste unitário confirma nova tentativa após colisão |
| T-06 | Aceitar código próprio com unicidade | RF-03, RN-01, RN-05, D-04 | T-05 | Testes unitários cobrem formato; código duplicado retorna 409 |
| T-07 | Redirecionar por código | RF-04, RN-03 | T-05, T-10 | Teste de integração cobre redirecionamento válido, inexistente e expirado |
| T-08 | Incrementar contador de forma atômica | RF-05, RN-04, D-02 | T-07 | Teste unitário confirma que o contador não diminui; teste concorrente não perde contagem e inclui acessos repetidos e automatizados |
| T-09 | Consultar estatísticas publicamente | RF-07 | T-05, T-08 | Teste de integração confirma que qualquer pessoa com o código recebe o total |
| T-10 | Aplicar expiração em UTC | RF-06, RN-03, borda de data passada | T-02, T-05 | Testes unitários cobrem data passada e limite UTC; link expira no início do dia UTC seguinte e não conta acesso |