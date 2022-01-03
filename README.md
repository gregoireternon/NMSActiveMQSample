# Routage des messages sur des MQ

## Introduction
Kafka est LE messaging dernier cri, dernier né de la famille, il a souvent tendance à faire oublier les autres. Les comparatifs visibles sur internet, rarement concrets, permettent peu de rendre compte de la différence entre celui-ci et d'autres outils de messaging plus "classiques".
En particulier, ces comparatifs mettent au second plan la notion de routage des messages (dont Kafka est globalement dépourvu).
Le routage étant rendu compliqué, la solution la plus immédiate est alors de faire un topic par type de message, et même par version majeure de format de message.
Le découplage entre le Producer et le Consumer est donc largement compromis, puisque chaque acteur doit savoir précisément à quel bus s'abonner pour chacune de ses fonctionnalités et même en fonction de la version des messages qui transitent dessus.

Si tout cela est vrai sur Kafka, ce n'est pas le cas sur des MQ plus "traditionnels"

Sur cette page, nous allons voir pourquoi, 
- en avançant d’abord une hypothèse de POC 
- en décrivant ensuite comment on y parvient

## Hypothèse

Objectifs: 
- Voir dans quelle mesure il est possible de découpler des services en utilisant le messaging
- Voir dans quelle mesure nous pouvons envisager les montées de version et même les “breaking changes” entre plusieurs microservices

 
Contexte:
- Tous les microservices sont interconnectés sur le même bus, et le même Topic (dans notre exemple, nous pourrons l’appeler “gestion_clous”, qui est un champ fonctionnel assez large, qui peut englober plusieurs actions
- L’objet fonctionnel de chaque message (“creation_client”,”client_cree”,”creation_devis”,…) est porté par le message lui même et pas par le topic
- L’abonnement à un “objet fonctionnel” dépend du client, mais le routage reste bien fait par le bus

Dans cette hypothèse, le bus n’est qu’une infrastructure réseau, dans le sens où il n’est pas nécessaire de modifier son paramétrage quand le besoin métier évolue

Le souci dans cette hypothèse est, entre autres, de bien garder séparer ce qui est lié au métier et ce qui est lié à l’infrastructure

 

 
## La limite Kafka

L'approche Kafka présente une limite dans la mise en oeuvre de ce POC; En effet sur Kafka, le client (consumer) doit lire ET être en mesure de traiter TOUS les messages publiés sur un topic donné; Dans le cas contraire, il plante. Cela implique que le Topic doit être créé en connaissance des données qu'il va transmettre; donc il ne peut être centré que sur une action ("creation_client" par exemple) et même sur une version de cette action dès lors que son message associé doit évoluer.
Comme nous l'avons vu lors de nos échanges, cela induit la nécessité de réfléchir à des contrats de messages suffisamment fortement typés pour garantir une bonne communication; et au bout du compte, cela nous contraint sur les acteurs (producer ou consumer) qui peuvent effectivement se connecter à un Topic Kafka; En conclusion, si le producer et le consumer sont nécessairement connus à l'avance, on peut en effet dire que le couplage est fort.
Cela est lié à l'architecture même de Kafka: il s'agit d'un Log que des consumer vont venir consulter en mode polling; il appartient au Consumer de savoir quel message il a lu ou non. Cette conception est particulièrement intéressante pour la tenue en charge puisqu'elle allège dramatiquement la charge du broker.
Cette conception est la grosse différence avec les Topics MQ qui fonctionnent pour la plupart en mode Push: dans leur cas c'est le broker qui pousse l'information vers le Consumer, et c'est lui qui sait vers quel Consumer pousser un message ou non.


## L'approche MQ /JMS

Dans le cadre de ce POC, nous prenons l'exemple de comment nous pourrions faire en utilisant un bus plus traditionnel. Pour l'exemple, j'ai pris ActiveMQ qui est une implémentation assez simple mais néanmoins robuste de JMS.
JMS (Java Message Service) est un standard de messaging dont il existe de nombreuses implémentations largement utilisées, pas nécessairement dans le monde Java, puisqu'il existe des clients dans de nombreux langages. Les plus connues sont notamment:

- Tibco EMS
- IBM MQ (anciennement et plus connu sous le nom de MQSeries)
- ActiveMQ dans le monde OpenSource

Sur ActiveMQ (et plus généralement en JMS), on peut faire transiter des messages assez riches composés:
- D'un body (plusieurs formats cont possibles: string, binaire, Map, Object ...)
- D'un header
- De properties

L'usage est de créer un Topic dont le périmètre est plus général qu'une simple action, car on peut ensuite spécifier l'action dans les propriétés du message.
De même, on peut renseigner de nombreuses autres informations dynamiquement et "par convention", dont, entre autres, et par exemple, la version du message utilisée.
Le Consumer peut ensuite être conçu pour traiter une et une seule action, à une version données, dans ce cas, il est tout à fait possible de renseigner un "sélector" au moment de la souscription, qui est une commande qui spécifie les paramètres d'abonnement demandés par le Consumer. Ces paramètres, définis sous la forme d'une requête "SQL like", sont ensuite interprétés par le broker lui même qui saura vers quel Consumer pousser le message.

![simple domain](mq.png?raw=true "Simple Domain")

Sur le schéma ci dessus:
- 2 MS Client différents écoutent les messages concernant les clients (“creation_client” mais aussi potentiellement d’autres messages)
- Les 2 MS Client sont en version différente: le V1 reçoit les messages en V1 le V2 reçoit les messages en V2; il est tout à fait possible de faire des MS qui prennent en charge plusieurs version d’un même message
- 1 MS Devis, branché sur le même Topic écoute les événements qui le concerne: création de devis
- Un MS Monitoring “technique” permet une supervision globale de tous les messages échangés entre les micro services et permet d’identifier des failles dans les échanges

 
## WildCard Routing

Active MQ Artemis (c’est aussi le cas de Tibco EMS, mais pour le coup je ne crois pas que ce soit dans le standard JMS) supporte également un autre mode de routage complémentaire (on peut les utiliser en plus du selector dont nous venons de parler): le WildCard

Cette technique consiste, en se basant sur des règles de nommages strictes des topics, en la possibilité de s’abonner à plusieurs topics fonctionnels différents. Un exemple intéressant pourrait être de partager un service transverse (MS client par exemple) entre plusieurs groupes de Microservices qui seraient sur des topics différents (exemple : “gestion_clous” et “gestion_agrafes”). Concrètement, on pourrait avoir l’architecture applicative suivante:

![multi Domain](mq-multi-domain.png?raw=true "Multi Domain")

Sur cet exemple, MSClient prend en charge les création de client à la fois sur le périmètre Clous et Agrafes, en toute transparence pour les autres microservices qui restent par ailleurs dans des périmètres indépendants.

 
## Implémentation de démonstration

Les concepts dont nous venons de parler sont disponibles dans ce projet 

 

Il s’agit de 3 projets dotnet 5 utilisant la brique NMS (client JMS) pour se connecter à un bus Active MQ artemis (https://activemq.apache.org/components/artemis/)

Ce sample contient:
- Deux projets Producers qui écrivent sur des topics différents (“myTopic.rouen” et ”myTopic.paris”)
- Un consumer qui est utilisé pour tester les différentes manières de recevoir les messages

A noter:
- Les producers écrivent des messages de différents type sur le même topic qui peuvent être interprétés programmatiquement au runtime par le consumer (text ou object message: un teste le message avant de le traiter; ça ça doit être possible avec Kafka aussi)
- Les producers écrivent les messages Object en y ajoutant une propriété (que l’on pourrait mettre aussi sur les messages Text) “Canal”; cette propriété peut prendre une valeur de 1 à 10.
- Le consumer est paramétré grâce au selector pour ne recevoir que les messages sur le Canal 2: il ignore donc tous les autres, sans effet de bord au niveau du broker qui ne conserve pas des messages qui ne seront in fine pas consommés
- Le consumer est paramétré grace au wildcard pour écouter les topics des deux producers en même temps (il écoute le topic “myTopic.#”)
- Les producers créent dynamiquement une queue qui leur appartient et qui leur sert de canal de réponse pour acquittement des services qui recevraient leur requête; ce mécanisme n’implique absolument pas une connaissance préalable et/ou réciproque des services

 
Notes:
* Les selectors n’existent pas sur Kafka, et en tout cas ne sont pas supportés par le Broker lui-même (c’est à dire qu’une implémentation serait possible sur le client)https://stackoverflow.com/questions/38660985/apache-kafka-client-with-selector 
* https://github.com/gregoireternon/NMSActiveMQSample  : Code source sample d’usage d’ActiveMQ avec:*
  * Selector (permettant de filter)
  * Routage (avec Wild cards) 
  * Queues temporaires
