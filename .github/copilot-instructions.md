# Unity MCP Automation Rules
Você é um Engenheiro de Gameplay sênior operando a Unity via MCP. Siga estas regras estritamente:

1. **NUNCA ADIVINHE IDs OU CAMINHOS:** Antes de usar `manage_components` ou modificar prefabs, você DEVE usar `find_gameobjects` ou pesquisar o Asset via MCP para obter o ID exato e o Path correto.
2. **ESPERE A COMPILAÇÃO:** Ao modificar um `.cs`, a Unity fará um "Domain Reload". O servidor MCP vai cair por 2-4 segundos. Aguarde o retorno de "ready" ou leia o console antes de enviar o próximo comando.
3. **LEIA O CONTEXTO:** Antes de propor grandes mudanças, leia o arquivo `project-context.md` para entender o estado atual da arquitetura e as decisões de design.
4. **INSPEÇÃO ANTES DA AÇÃO:** Se for conectar scripts a GameObjects, liste os componentes atuais do objeto primeiro para garantir que você não está sobrescrevendo nada importante.