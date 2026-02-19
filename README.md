Sistema de Detecção e Alerta de Alagamentos Urbanos

Projeto desenvolvido no SENAI – Serviço Nacional de Aprendizagem Industrial com foco em monitoramento ambiental e apoio à mobilidade urbana.

A solução utiliza um sensor resistivo de nível de água integrado a um microcontrolador para coletar dados físicos do ambiente. As leituras são convertidas em níveis de alagamento e transmitidas via comunicação serial para uma aplicação desktop desenvolvida em C# (.NET/WPF). O sistema interpreta os valores recebidos, classifica o grau de risco e exibe notificações em tempo real em um mapa interativo.

A interface permite autenticação de usuários, visualização do estado das vias monitoradas e consulta de trajetos seguros. Quando um trecho apresenta alagamento, a aplicação realiza automaticamente o recálculo da rota, desviando da área crítica por meio da integração com serviços de geolocalização da Google.

Os dados de usuários, sensores e notificações são armazenados em banco de dados relacional MySQL, garantindo persistência e histórico de ocorrências. A arquitetura segue o modelo cliente-servidor, com processamento local na aplicação e armazenamento centralizado no banco.

Testes de validação indicaram alta confiabilidade na leitura dos níveis de água e funcionamento consistente do sistema de alerta e desvio de rotas.

Tecnologias utilizadas: C#, .NET WPF, MySQL, comunicação serial, sensores físicos e conceitos de IoT.
Objetivo: fornecer informações em tempo real para prevenção de riscos e apoio à tomada de decisão em áreas sujeitas a alagamentos.

Todos os direitos reservado as criadoras - Ana Paula Silva de Moraes & Yasmin Pereira dos Santos - 2025
