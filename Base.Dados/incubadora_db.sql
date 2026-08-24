-- ======================================================================
-- INCUBADORA INTELIGENTE - BASE DE DADOS MYSQL
-- Baseado no diagrama de classes (Avicultor/Administrador, Dispositivo,
-- Fase, Alerta, Diagnostico, System) e no JSON atualmente enviado
-- pelo firmware ESP32 (INCUBADORA INTELIGENTE - WOKWI FINAL v2).
--
-- Suporta MULTIPLOS dispositivos/incubadoras e multiplos utilizadores.
-- ======================================================================

CREATE DATABASE IF NOT EXISTS incubadora_db
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE incubadora_db;

SET FOREIGN_KEY_CHECKS = 0;

-- ======================================================================
-- 1. UTILIZADORES
-- Classes "Avicultor" e "Administrador" do diagrama fundem-se aqui
-- numa unica tabela com um campo `papel`, para evitar duplicar
-- estrutura entre as duas (ambas tem nome/credenciais/autenticar()).
-- ======================================================================

CREATE TABLE utilizadores (
  id               INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  nome             VARCHAR(120)        NOT NULL,
  email            VARCHAR(190)        NOT NULL,
  contacto         VARCHAR(60)         NULL,
  credenciais_hash VARCHAR(255)        NOT NULL COMMENT 'hash da password (bcrypt/argon2), nunca texto simples',
  papel            ENUM('avicultor', 'administrador') NOT NULL DEFAULT 'avicultor',
  ativo            TINYINT(1)          NOT NULL DEFAULT 1,
  criado_em        DATETIME            NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em    DATETIME            NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uk_utilizadores_email (email)
) ENGINE=InnoDB;


-- ======================================================================
-- 2. DISPOSITIVOS
-- Classe "Dispositivo". Cada incubadora fisica e uma linha aqui.
-- O "Device_mod" (display/LCD) e tratado como um MODULO do dispositivo,
-- nao como entidade propria, porque na pratica e sempre 1-para-1 com
-- a incubadora e nao tem historico proprio significativo -- apenas
-- guarda o seu proprio estado de ligacao/autenticacao local.
-- ======================================================================

CREATE TABLE dispositivos (
  id                  INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  nome                VARCHAR(120)   NOT NULL COMMENT 'nome amigavel dado pelo utilizador, ex: "Incubadora Galpao 1"',
  tipo                VARCHAR(60)    NOT NULL DEFAULT 'incubadora',
  numero_serie        VARCHAR(80)    NOT NULL COMMENT 'ex: MAC address do ESP32',
  localizacao         VARCHAR(160)   NULL,
  estado              ENUM('ligado','desligado','erro','ciclo_concluido') NOT NULL DEFAULT 'desligado',
  ultima_leitura_em   DATETIME       NULL,

  -- campos do "Device_mod" (display) embutidos como modulo do dispositivo
  display_presente    TINYINT(1)     NOT NULL DEFAULT 1,
  display_estado      ENUM('ok','falha','desconhecido') NOT NULL DEFAULT 'desconhecido',
  display_ultima_auth DATETIME       NULL COMMENT 'ultima vez que alguem autenticou localmente no LCD/teclado, se aplicavel',

  criado_em           DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em       DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uk_dispositivos_numero_serie (numero_serie)
) ENGINE=InnoDB;


-- ======================================================================
-- 3. UTILIZADORES <-> DISPOSITIVOS (N:N)
-- Cobre as relacoes "configura" (Avicultor/Administrador -> Dispositivo)
-- e "gerirDispositivos()" do Administrador. Um utilizador pode gerir
-- varios dispositivos e um dispositivo pode ter varios responsaveis.
-- ======================================================================

CREATE TABLE utilizadores_dispositivos (
  utilizador_id  INT UNSIGNED NOT NULL,
  dispositivo_id INT UNSIGNED NOT NULL,
  papel_no_dispositivo ENUM('proprietario','gestor','observador') NOT NULL DEFAULT 'gestor',
  associado_em   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (utilizador_id, dispositivo_id),
  CONSTRAINT fk_ud_utilizador  FOREIGN KEY (utilizador_id)  REFERENCES utilizadores(id) ON DELETE CASCADE,
  CONSTRAINT fk_ud_dispositivo FOREIGN KEY (dispositivo_id) REFERENCES dispositivos(id) ON DELETE CASCADE
) ENGINE=InnoDB;


-- ======================================================================
-- 4. FASES
-- Classe "Fase". Representa um CICLO de incubacao de um dispositivo
-- (dia 1 a 22). Cada novo ciclo/nascimento cria uma nova linha, o que
-- automaticamente da historico entre incubacoes sucessivas no mesmo
-- dispositivo.
-- ======================================================================

CREATE TABLE fases (
  id             INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  dispositivo_id INT UNSIGNED NOT NULL,
  tipo           ENUM('incubacao','eclosao','pos_eclosao') NOT NULL,
  nome           VARCHAR(80)  NULL COMMENT 'ex: "Ciclo Agosto 2026"',
  data_inicio    DATETIME     NOT NULL,
  data_fim       DATETIME     NULL,
  estado         ENUM('ativa','terminada') NOT NULL DEFAULT 'ativa',
  CONSTRAINT fk_fases_dispositivo FOREIGN KEY (dispositivo_id) REFERENCES dispositivos(id) ON DELETE CASCADE,
  KEY idx_fases_dispositivo_estado (dispositivo_id, estado)
) ENGINE=InnoDB;


-- ======================================================================
-- 5. LEITURAS
-- Dados enviados periodicamente pelo ESP32 (funcao criarJSON() do
-- firmware). Alinhado ao JSON REAL atual: projeto, dia, fase,
-- temperatura, humidade, estado_temperatura, estado_humidade,
-- alerta, rotacao_ativa. (Os campos antigos tipo_ovo/ds18b20/servo/modo
-- do Flask antigo ja NAO existem no firmware v2 e foram removidos.)
-- ======================================================================

CREATE TABLE leituras (
  id                  BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  dispositivo_id      INT UNSIGNED NOT NULL,
  fase_id             INT UNSIGNED NULL,
  dia                 TINYINT UNSIGNED NOT NULL,
  fase_nome           VARCHAR(30)  NOT NULL COMMENT 'snapshot textual: incubacao/eclosao/pos_eclosao',
  temperatura         DECIMAL(5,2) NULL,
  humidade            DECIMAL(5,2) NULL,
  estado_temperatura  ENUM('baixa','adequada','alta','falha_sensor','ciclo_concluido','desconhecido') NOT NULL DEFAULT 'desconhecido',
  estado_humidade     ENUM('baixa','adequada','alta','falha_sensor','ciclo_concluido','desconhecido') NOT NULL DEFAULT 'desconhecido',
  rotacao_ativa       TINYINT(1)   NOT NULL DEFAULT 0,
  alerta              VARCHAR(40)  NOT NULL DEFAULT 'nenhum',
  data_hora           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

  CONSTRAINT fk_leituras_dispositivo FOREIGN KEY (dispositivo_id) REFERENCES dispositivos(id) ON DELETE CASCADE,
  CONSTRAINT fk_leituras_fase        FOREIGN KEY (fase_id)        REFERENCES fases(id)        ON DELETE SET NULL,
  KEY idx_leituras_dispositivo_data (dispositivo_id, data_hora),
  KEY idx_leituras_fase (fase_id)
) ENGINE=InnoDB;


-- ======================================================================
-- 6. ALERTAS
-- Classe "Alerta". Uma linha por alerta gerado (temperatura_alta,
-- humidade_baixa, falha_sensor, etc.). Liga-se opcionalmente a
-- leitura que o despoletou.
-- ======================================================================

CREATE TABLE alertas (
  id             INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  dispositivo_id INT UNSIGNED NOT NULL,
  leitura_id     BIGINT UNSIGNED NULL,
  tipo           VARCHAR(40)  NOT NULL COMMENT 'ex: temperatura_alta, temperatura_baixa, humidade_alta, humidade_baixa, falha_sensor',
  gravidade      ENUM('baixa','media','alta','critica') NOT NULL DEFAULT 'media',
  mensagem       VARCHAR(255) NOT NULL,
  estado         ENUM('ativo','resolvido') NOT NULL DEFAULT 'ativo',
  data_hora      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  resolvido_por  INT UNSIGNED NULL,
  resolvido_em   DATETIME     NULL,

  CONSTRAINT fk_alertas_dispositivo FOREIGN KEY (dispositivo_id) REFERENCES dispositivos(id) ON DELETE CASCADE,
  CONSTRAINT fk_alertas_leitura     FOREIGN KEY (leitura_id)     REFERENCES leituras(id)     ON DELETE SET NULL,
  CONSTRAINT fk_alertas_resolvido_por FOREIGN KEY (resolvido_por) REFERENCES utilizadores(id) ON DELETE SET NULL,
  KEY idx_alertas_dispositivo_estado (dispositivo_id, estado)
) ENGINE=InnoDB;


-- ======================================================================
-- 7. NOTIFICACOES DE ALERTAS
-- Cobre "receberNotificacao(alerta)" / "visualizarAlerta()" do
-- Avicultor: registo de para quem cada alerta foi notificado e se
-- ja foi visto.
-- ======================================================================

CREATE TABLE alertas_notificacoes (
  alerta_id      INT UNSIGNED NOT NULL,
  utilizador_id  INT UNSIGNED NOT NULL,
  notificado_em  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  visualizado    TINYINT(1)   NOT NULL DEFAULT 0,
  visualizado_em DATETIME     NULL,
  PRIMARY KEY (alerta_id, utilizador_id),
  CONSTRAINT fk_an_alerta     FOREIGN KEY (alerta_id)     REFERENCES alertas(id)      ON DELETE CASCADE,
  CONSTRAINT fk_an_utilizador FOREIGN KEY (utilizador_id) REFERENCES utilizadores(id) ON DELETE CASCADE
) ENGINE=InnoDB;


-- ======================================================================
-- 8. DIAGNOSTICOS
-- Classe "Diagnostico". Resultado de analises (ex: deteccao de
-- padroes de falha, recomendacoes) sobre um dispositivo/fase.
-- ======================================================================

CREATE TABLE diagnosticos (
  id            INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  dispositivo_id INT UNSIGNED NOT NULL,
  fase_id        INT UNSIGNED NULL,
  resultado      VARCHAR(120) NOT NULL,
  confianca      DECIMAL(5,2) NULL COMMENT 'percentagem 0-100',
  recomendacao   TEXT         NULL,
  data_hora      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  gerado_por     INT UNSIGNED NULL COMMENT 'utilizador que pediu diagnosticar(), NULL se automatico',

  CONSTRAINT fk_diag_dispositivo FOREIGN KEY (dispositivo_id) REFERENCES dispositivos(id) ON DELETE CASCADE,
  CONSTRAINT fk_diag_fase        FOREIGN KEY (fase_id)        REFERENCES fases(id)        ON DELETE SET NULL,
  CONSTRAINT fk_diag_gerado_por  FOREIGN KEY (gerado_por)     REFERENCES utilizadores(id) ON DELETE SET NULL,
  KEY idx_diag_dispositivo (dispositivo_id)
) ENGINE=InnoDB;

SET FOREIGN_KEY_CHECKS = 1;


-- ======================================================================
-- VIEW DE APOIO: ultima leitura por dispositivo (equivalente ao antigo
-- endpoint /dados/ultima, mas agora por dispositivo)
-- ======================================================================

CREATE OR REPLACE VIEW vw_ultima_leitura_por_dispositivo AS
SELECT l.*
FROM leituras l
INNER JOIN (
  SELECT dispositivo_id, MAX(id) AS max_id
  FROM leituras
  GROUP BY dispositivo_id
) ultimas ON ultimas.dispositivo_id = l.dispositivo_id AND ultimas.max_id = l.id;
