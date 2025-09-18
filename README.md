# MU Launcher

## Visão geral
O MU Launcher é um aplicativo Windows Forms (.NET Framework) que executa atualizações do jogo e exibe notícias em um navegador embutido. O layout da interface foi externalizado para um arquivo XML (`layout/launcher.layout.xml`), permitindo trocar imagens, botões e textos sem recompilar o executável principal. Esse arquivo descreve dimensões do formulário, controles estáticos e botões com múltiplos estados visuais que são aplicados em tempo de execução quando o launcher é iniciado.【F:Launcher/pForm.cs†L21-L66】【F:Launcher/layout/launcher.layout.xml†L1-L19】【F:Launcher/Layout/LayoutRuntime.cs†L248-L395】

Além do executável, o repositório inclui o **Launcher Layout Editor**, uma ferramenta WinForms dedicada para editar o layout de forma visual. Ela oferece uma superfície de design com pré-visualização do fundo, grade de propriedades categorizada e suporte a arrastar botões dinâmicos, salvando novamente no formato consumido pelo launcher.【F:Launcher.LayoutEditor/MainForm.cs†L10-L171】【F:Launcher.LayoutEditor/MainForm.cs†L365-L442】

## Estrutura do repositório
- `Launcher/` – projeto principal do launcher que continua carregando lógica de atualização, leitura de `mu.ini` e agora aplica o layout descrito em XML, instanciando botões dinâmicos e estados visuais conforme as configurações externas.【F:Launcher/pForm.cs†L24-L361】【F:Launcher/Layout/LayoutRuntime.cs†L248-L533】
- `Launcher/layout/` – pasta padrão pesquisada pelo executável em runtime; contém o XML `launcher.layout.xml` e deve armazenar também as imagens referenciadas (subpasta `assets/` por padrão).【F:Launcher/pForm.cs†L46-L56】【F:Launcher/layout/launcher.layout.xml†L1-L19】
- `Launcher.LayoutEditor/` – projeto da ferramenta visual que reutiliza as definições de layout, permite abrir/salvar o XML e manipular controles/botões com uma interface do tipo designer.【F:Launcher.LayoutEditor/Launcher.LayoutEditor.csproj†L1-L37】【F:Launcher.LayoutEditor/MainForm.cs†L24-L171】

## Requisitos
- **Visual Studio 2019 ou 2022** (ou MSBuild compatível) com suporte ao .NET Framework.
- **.NET Framework 2.0** para compilar e executar o projeto `Launcher` original.【F:Launcher/Launcher.csproj†L3-L34】
- **.NET Framework 4.8** para compilar o projeto `Launcher.LayoutEditor`, que depende de componentes de design modernos.【F:Launcher.LayoutEditor/Launcher.LayoutEditor.csproj†L3-L31】

## Como compilar
1. Abra `Launcher.sln` no Visual Studio.
2. Garanta que os pacotes alvo (NET 2.0 e 4.8) estejam instalados na máquina.
3. Compile a solução inteira; o Visual Studio gerará `Launcher.exe` na pasta `Launcher/bin/<Config>/` e `Launcher.LayoutEditor.exe` na pasta `Launcher.LayoutEditor/bin/<Config>/`.
4. Se desejar distribuir apenas o launcher final, basta desmarcar o projeto do editor na configuração ou ignorar o executável gerado.

## Funcionamento do sistema de layout
### Carregamento no launcher
Ao iniciar, `pForm` procura a pasta `layout` ao lado do executável e, se encontrar `launcher.layout.xml`, carrega as definições por meio de `LayoutFile.Load`. O runtime aplica tamanho da janela, imagem de fundo, ícone e transparência, além de localizar controles existentes pelo `Name` para sobrescrever texto, imagens e posicionamento. Caso o arquivo não exista, o launcher continua exibindo o layout compilado nos arquivos `.Designer` originais.【F:Launcher/pForm.cs†L39-L66】【F:Launcher/Layout/LayoutRuntime.cs†L248-L395】

### Estrutura do arquivo de layout
O XML define propriedades do formulário (`<form>`), controles estáticos (`<control>`), botões com skins (`<imageButton>`/`<toggleButton>`) e botões dinâmicos (`<dynamicButton>`). Cada elemento referencia imagens relativas à pasta declarada em `basePath` (por padrão `assets`).【F:Launcher/layout/launcher.layout.xml†L1-L19】【F:Launcher/Layout/LayoutDefinition.cs†L19-L184】

- **Controles estáticos** (`LayoutControl`) permitem configurar posição, tamanho, textos, fontes, cores, imagens de fundo/primeiro plano e URL para controles como `Label`, `PictureBox`, `WebBrowser` e `Panel` já existentes no formulário original.【F:Launcher/Layout/LayoutDefinition.cs†L101-L121】【F:Launcher/Layout/LayoutRuntime.cs†L446-L533】
- **Botões com skin** (`imageButton`/`toggleButton`) associam estados visuais (normal, hover, pressionado, desabilitado, marcado) a `PictureBox` existentes. O runtime registra esses skins e atualiza automaticamente a imagem ao mudar o estado lógico.【F:Launcher/Layout/LayoutDefinition.cs†L154-L171】【F:Launcher/Layout/LayoutRuntime.cs†L358-L395】
- **Botões dinâmicos** (`dynamicButton`) criam novos `PictureBox` em tempo de execução, com posicionamento, dimensões e ação definidos no XML. Eles suportam múltiplos sprites, modos de escala e podem ser ativados/desativados por configuração.【F:Launcher/Layout/LayoutDefinition.cs†L173-L184】【F:Launcher/Layout/LayoutRuntime.cs†L396-L443】

### Ações disponíveis
Quando um botão dinâmico é clicado, o launcher executa a ação nomeada no XML. A implementação padrão inclui comandos para disparar o jogo (`Start`), fechar (`Exit`), abrir opções (`Options`/`ShowOptions`), alternar modo janela (`ToggleWindowMode`), alternar o estado visual (`ToggleState`), minimizar (`Minimize`), abrir URLs (`OpenUrl`), iniciar arquivos ou pastas (`Run`/`Launch`/`OpenFile`/`OpenFolder`) e mostrar mensagens (`Message`). Parâmetros adicionais podem ser passados pelo atributo `argument` e são encaminhados para o método correspondente.【F:Launcher/pForm.cs†L232-L361】

## Usando o Launcher Layout Editor
1. **Abrir/Salvar layouts** – Use os comandos *File → Open/Save/Save As* para carregar ou persistir arquivos `.layout.xml`. O editor mantém o caminho de origem e atualiza a pré-visualização sempre que um layout é carregado ou salvo.【F:Launcher.LayoutEditor/MainForm.cs†L38-L171】
2. **Definir a pasta de assets** – Pelo menu *Layout → Set Asset Folder*, selecione a raiz dos recursos. O editor grava o caminho relativo no XML e usa essa pasta para resolver imagens durante a pré-visualização.【F:Launcher.LayoutEditor/MainForm.cs†L90-L115】
3. **Pré-visualização visual** – A janela principal cria uma superfície de design com o fundo configurado, posiciona controles estáticos e botões dinâmicos e mantém um overlay de seleção para feedback visual.【F:Launcher.LayoutEditor/MainForm.cs†L365-L442】
4. **Grade de propriedades** – Ao selecionar um elemento, a `PropertyGrid` expõe propriedades categorizadas (Layout, Appearance, Behavior, Visuals). Para formulários e controles, os editores personalizados convertem caminhos absolutos em relativos à pasta de assets.【F:Launcher.LayoutEditor/PropertyModels.cs†L16-L207】【F:Launcher.LayoutEditor/RelativePathEditor.cs†L9-L117】
5. **Botões dinâmicos** – É possível criar novos botões pelo menu *Add Dynamic Button*; eles surgem com valores padrão e podem ser movidos com o mouse dentro da superfície. Ao soltar, o editor persiste a nova posição no layout.【F:Launcher.LayoutEditor/MainForm.cs†L127-L285】
6. **Arquivo limpo** – Sempre que uma propriedade muda, o editor marca o documento como modificado, atualiza a visualização e alerta sobre alterações não salvas ao fechar.【F:Launcher.LayoutEditor/MainForm.cs†L160-L299】

## Fluxo de trabalho sugerido
1. Copie o layout base (`Launcher/layout/launcher.layout.xml`) e a pasta `assets/` para um diretório de trabalho ou projeto de tema.【F:Launcher/layout/launcher.layout.xml†L1-L19】
2. Abra o arquivo copiado no Launcher Layout Editor, defina a pasta de assets e ajuste controles/botões conforme necessário (textos, fontes, skins, ações).【F:Launcher.LayoutEditor/MainForm.cs†L90-L171】【F:Launcher.LayoutEditor/PropertyModels.cs†L16-L207】
3. Salve o layout e teste abrindo o `Launcher.exe`; o aplicativo carregará automaticamente o novo XML e aplicará as imagens relativas ao `basePath` configurado.【F:Launcher/pForm.cs†L39-L66】【F:Launcher/Layout/LayoutRuntime.cs†L248-L443】
4. Repita o processo até atingir o visual desejado. Ao finalizar, distribua o executável do launcher junto com o arquivo de layout e os assets referenciados.

## Distribuição
Para publicar o launcher customizado, inclua os seguintes itens na pasta final:
- `Launcher.exe` (e dependências padrão do .NET Framework 2.0).
- Pasta `layout/` contendo `launcher.layout.xml` e o diretório `assets/` com todas as imagens usadas por controles e botões.【F:Launcher/pForm.cs†L46-L56】【F:Launcher/layout/launcher.layout.xml†L1-L19】

Se a pasta ou o XML não estiverem presentes, o aplicativo volta automaticamente ao layout embutido, garantindo compatibilidade mesmo sem os arquivos externos.【F:Launcher/pForm.cs†L49-L66】

