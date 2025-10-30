## GameWorker - EN
This is an experimental project created with the ideia of bringing some qualities of the consoles to the gaming PC. The ideia is to run a physical media in a modern gaming PC using an USB device to store the game. This project will read the config files inside the USB device, which stores the .exe file path, and will start the proccess.

# Install
1) For the correct execution of the service, drop the files inside
this directory: "C:\Games\GameWorker\" (Cannot change the path)

2) Right-click the file "GamingWorkerService.exe" 
and select the option "Properties"

3) Check the option "Run this program as Administrator" in tab "Compatibility" 

4) Once the steps above are completed, run the script bellow as Admin:
"CriarTarefa.bat"

Restart your PC for the changes be applied correctly!

Now the service is installed and the game should run as you plug your
USB device in your PC.

# USB device Setup

I strongly recommend you to use a Micro SD card with an USB SD Reader. If you wanna play some heavy games (open world RPGs for example). It makes a lot of random access to the storage device and some slow devices like a simple Flash Drive or an HDD can suffer a lot and make the game unplayable. Look for Class 10 MicroSD with A1 or A2 classification.

You need only two files to configure your USB device to run a game.
1) run.txt
2) args.txt

I will use YUZU as an example. I usually create a folder called "ROMS" inside my emulators directories.
So, if you have a "Zelda.nsp" inside the "ROMS" folder.

Your run.txt file should contains something like "Yuzu\yuzu.exe".
Now your args.txt file should contains something like "-f -g '.\ROMS\Zelda.nsp'"

Where *-f* starts the game in fullscreen, and *-g* is to inform the directory of the desired rom.

Once you plug your USB device, the GameWorker should read these two files. Start the proccess and apply the arguments.

*Note*: If your game do not need arguments to start. Then create just the "run.txt" file.

## GameWorker - PTBR
Esse é um projeto experimental criado a partir da ideia de trazer algumas qualidade dos consoles para o PC Gamer. A ideia é roda uma mídia fisica em um PC Gamer moderno usando um dispositivo USB para armazenar o jogos. O projeto deve ler os arquivos de configuração dentro do dispositivo USB, que contém o caminho para o arquivo .exe, e inicia o processo.

# Instalação
1) Para correta execução do serviço, jogue o conteudo
dessa pasta em: "C:\Games\GameWorker\" (Precisa ser esse caminho obrigatoriamente)

2) Clique com o botão direito no arquivo "GamingWorkerService.exe" 
e selecione a opção "Propriedades"

3) Marque a opção "Executar esse programa como administrador"
na aba "Compatibilidade" 

4) Ao completar os passos acima, execute como administrador o script:
"CriarTarefa.bat" 

Reinicie seu computador para que as mudanças surtam efeito!

Agora o serviço está instalado e o jogo deve ser executado ao plugar
seu dispositivo USB.

# Configurando o dispositivo USB

Eu recomendo fortemente usar um Micro SD com um Leitor USB de SD. Se você quiser jogar jogos pesados (RPGs de mundo aberto por exemplo). Eles fazem muitos acessos aleatórios ao armazenamento e alguns dispositivos mais lentos como Pendrives e HDDs podem sofrer muito e tornar o jogo injogável. Procure por um MicroSD Classe 10 com classificação A1 ou A2

Você precisa apenas de dois arquivos de configuração no seu dispositivo:
1) run.txt
2) args.txt

Vou usar o YUZU como exemplo. Eu normalmente crio uma pasta "ROMS" dentro da pasta do emulador.
Então, se você tiver um "Zelda.nsp" dentro da pasta "ROMS".

Seu run.txt deve conter algo como "Yuzu\yuzu.exe".
E seu args.txt deve conter algo como "-f -g '.\ROMS\Zelda.nsp'"

Onde *-f* inicia o jogo em tela cheia, e *-g* é para informar o diretorio da rom desejada.

Quando você plugar o dispositivo USB, o GameWorker deve ler esses dois arquivos e iniciar o processo aplicando os argumentos.

*Observação*: Se seu jogo não precisa de argumentos pra iniciar, crie apenas o arquivo "run.txt".
