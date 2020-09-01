import _ from "lodash";
import * as React from "react";
import { Link, LinkDropdown } from "ui/components";

import CardIcon from "@skbkontur/react-icons/Card";
import DocumentLiteIcon from "@skbkontur/react-icons/DocumentLite";
import DownloadIcon from "@skbkontur/react-icons/Download";
import ExportIcon from "@skbkontur/react-icons/Export";
import InfoIcon from "@skbkontur/react-icons/Info";
import { MessageCardApiUrls } from "Domain/EDI/Api/MessageMonitoring/MessageCardApi";

const LinkMenuItem = LinkDropdown.MenuItem;

export const endsWith = (ending: string, str: string): boolean => str.slice(-ending.length) === ending;

function getByPath(target: Nullable<Object>, path: string[]): mixed {
    return _.get(target, path);
}

export function OrganizationLinkDropdown({
    partyId,
    caption,
}: {
    partyId: string;
    caption?: Nullable<string>;
}): JSX.Element {
    return (
        <LinkDropdown renderTitle={caption != null ? caption : partyId} data-tid="GoToLink">
            <LinkMenuItem href={`/AdminTools/Parties/${partyId}`} data-tid="GoToPartyEdit">
                <CardIcon /> Открыть карточку организации
            </LinkMenuItem>
            <LinkMenuItem
                href={`/AdminTools/BusinessObjects/PartyThrift/details?ScopeId=${partyId}&Id=${partyId}`}
                data-tid="GoToPartyBusinessObject">
                <InfoIcon /> Открыть бизнес-объект
            </LinkMenuItem>
            <LinkMenuItem href={`/${partyId}/Supplier/NewApplications`} data-tid="GoToPartySupplierInterface">
                <ExportIcon /> Открыть интерфейс поставщика
            </LinkMenuItem>
            <LinkMenuItem href={`/${partyId}/Monitoring/TaskChainList`} data-tid="GoToPartyMonitoring">
                <ExportIcon /> Открыть мониторинг сообщений
            </LinkMenuItem>
        </LinkDropdown>
    );
}

// eslint-disable-next-line max-statements
export function taskDetailsCustomRender(target: mixed, path: string[]): JSX.Element | null {
    const pathTop = path[path.length - 1];
    if (target == null) {
        return null;
    }
    if ((endsWith("PartyId", pathTop) || pathTop === "partyId") && typeof target === "object") {
        const partyId = getByPath(target, path);
        if (typeof partyId === "string") {
            return <OrganizationLinkDropdown partyId={partyId} />;
        }
    }

    if (
        (pathTop === "connectorBoxId" || endsWith("connectorBoxId", pathTop) || endsWith("ConnectorBoxId", pathTop)) &&
        typeof target === "object"
    ) {
        const connectorBoxId = getByPath(target, path);
        if (typeof connectorBoxId === "string") {
            return getBusinessObjectLink("ConnectorBoxStorageElement", connectorBoxId);
        }
    }

    if (pathTop === "transportBoxId" && typeof target === "object") {
        const id = getByPath(target, path);
        if (typeof id === "string") {
            return getBusinessObjectLink("TransportBoxStorageElement", id);
        }
    }

    if (
        (pathTop === "boxId" || endsWith("BoxId", pathTop)) &&
        typeof target === "object" &&
        pathTop !== "transportBoxId"
    ) {
        const boxId = getByPath(target, path);
        if (typeof boxId === "string") {
            if (containsEntityIdWithProp(target, "transportMessageId")) {
                return getBusinessObjectLink("TransportBoxStorageElement", boxId);
            }
            if (containsEntityIdWithProp(target, "connectorMessageId")) {
                return getBusinessObjectLink("ConnectorBoxStorageElement", boxId);
            }
            return getBusinessObjectLink("BoxStorageElement", boxId);
        }
    }

    if (pathTop === "id" && typeof target === "object") {
        const id = getByPath(target, path);
        if (typeof id === "string") {
            if (["deliveryBox", "transportBox"].includes(path[path.length - 2])) {
                return getBusinessObjectLink("TransportBoxStorageElement", id);
            }

            if (["inbox", "outbox", "box"].includes(path[path.length - 2])) {
                return getBusinessObjectLink("BoxStorageElement", id);
            }
        }
    }

    if (_.isEqual(path, ["computedConnectorInteractionId"]) && typeof target === "object") {
        if (
            typeof target.computedConnectorBoxId === "string" &&
            typeof target.computedConnectorInteractionId === "string"
        ) {
            return (
                <Link
                    data-tid="GoToLink"
                    href={
                        "/AdminTools/BusinessObjects/ConnectorInteractionContextStorageElement/details?ScopeId=" +
                        `${target.computedConnectorBoxId}&Id=${target.computedConnectorInteractionId}`
                    }>
                    {target.computedConnectorInteractionId}
                </Link>
            );
        }
    }

    if (_.isEqual(path, ["fullDiadocPackageIdentifiers", "invoiceEntityId"]) && typeof target === "object") {
        if (target.fullDiadocPackageIdentifiers != null && typeof target.fullDiadocPackageIdentifiers === "object") {
            const id = target.fullDiadocPackageIdentifiers.boxId;
            const letterId = target.fullDiadocPackageIdentifiers.messageId;
            const documentId = target.fullDiadocPackageIdentifiers.invoiceEntityId;
            if (typeof id === "string" && typeof letterId === "string" && typeof documentId === "string") {
                return (
                    <Link
                        data-tid="GoToLink"
                        href={
                            `https://diadoc.kontur.ru/${id}/Document/Show?letterId=${letterId}&` +
                            `documentId=${documentId}`
                        }>
                        {documentId}
                    </Link>
                );
            }
        }
    }

    if (_.isEqual(path, ["fullDiadocPackageIdentifiers", "invoiceCorrectionEntityId"]) && typeof target === "object") {
        if (target.fullDiadocPackageIdentifiers != null && typeof target.fullDiadocPackageIdentifiers === "object") {
            const id = target.fullDiadocPackageIdentifiers.boxId;
            const letterId = target.fullDiadocPackageIdentifiers.messageId;
            const documentId = target.fullDiadocPackageIdentifiers.invoiceCorrectionEntityId;
            if (typeof id === "string" && typeof letterId === "string" && typeof documentId === "string") {
                return (
                    <Link
                        data-tid="GoToLink"
                        href={
                            `https://diadoc.kontur.ru/${id}/Document/Show?letterId=${letterId}&` +
                            `documentId=${documentId}`
                        }>
                        {documentId}
                    </Link>
                );
            }
        }
    }

    if (_.isEqual(path, ["fullDiadocPackageIdentifiers", "torg12EntityId"]) && typeof target === "object") {
        if (target.fullDiadocPackageIdentifiers != null && typeof target.fullDiadocPackageIdentifiers === "object") {
            const id = target.fullDiadocPackageIdentifiers.boxId;
            const letterId = target.fullDiadocPackageIdentifiers.messageId;
            const documentId = target.fullDiadocPackageIdentifiers.torg12EntityId;
            if (typeof id === "string" && typeof letterId === "string" && typeof documentId === "string") {
                return (
                    <Link
                        data-tid="GoToLink"
                        href={
                            `https://diadoc.kontur.ru/${id}/Document/Show?letterId=${letterId}&` +
                            `documentId=${documentId}`
                        }>
                        {documentId}
                    </Link>
                );
            }
        }
    }

    if (
        _.isEqual(path, ["fullDiadocPackageIdentifiers", "universalTranferDocumentEntityId"]) &&
        typeof target === "object"
    ) {
        if (target.fullDiadocPackageIdentifiers != null && typeof target.fullDiadocPackageIdentifiers === "object") {
            const id = target.fullDiadocPackageIdentifiers.boxId;
            const letterId = target.fullDiadocPackageIdentifiers.messageId;
            const documentId = target.fullDiadocPackageIdentifiers.universalTranferDocumentEntityId;
            if (typeof id === "string" && typeof letterId === "string" && typeof documentId === "string") {
                return (
                    <Link
                        data-tid="GoToLink"
                        href={
                            `https://diadoc.kontur.ru/${id}/Document/Show?letterId=${letterId}&` +
                            `documentId=${documentId}`
                        }>
                        {documentId}
                    </Link>
                );
            }
        }
    }

    if (_.isEqual(path, ["documentCirculationId"]) && typeof target === "object") {
        if (typeof target.documentCirculationId === "string") {
            return (
                <Link data-tid="GoToLink" href={`/Monitoring/TaskChainList/Document/${target.documentCirculationId}`}>
                    {target.documentCirculationId}
                </Link>
            );
        }
    }

    if (pathTop === "documentCirculationId" && typeof target === "object") {
        const documentCirculationId = getByPath(target, path);
        if (typeof documentCirculationId === "string") {
            return (
                <Link data-tid="GoToLink" href={`/Monitoring/TaskChainList/Document/${documentCirculationId}`}>
                    {documentCirculationId}
                </Link>
            );
        }
    }

    if (pathTop === "rawMessageId" && typeof target === "object") {
        const rawMessageId = getByPath(target, path);
        const transportBoxId = getByPath(target, [...path.slice(0, path.length - 1), "transportBox", "id"]);
        if (typeof rawMessageId === "string" && typeof transportBoxId === "string") {
            return (
                <LinkDropdown renderTitle={rawMessageId}>
                    <LinkMenuItem
                        href={`/AdminTools/MessagesMeta?transportBoxId=${transportBoxId}&rawMessageId=${rawMessageId}&type=raw-meta`}>
                        <DocumentLiteIcon /> Открыть RawMessageMetaInformation
                    </LinkMenuItem>
                    <LinkMenuItem href={MessageCardApiUrls.getUrlForOpenTransportMessage(transportBoxId, rawMessageId)}>
                        <DownloadIcon /> Скачать файл
                    </LinkMenuItem>
                </LinkDropdown>
            );
        }
    }

    if (pathTop === "blobId" && typeof target === "object") {
        const blobId = getByPath(target, path);
        const blobSize = getByPath(target, [...path.slice(0, path.length - 1), "blobSize"]);
        const isLargeBlob = getByPath(target, [...path.slice(0, path.length - 1), "isLargeBlob"]);
        if (typeof blobId === "string" && typeof blobSize === "number" && typeof isLargeBlob === "boolean") {
            return (
                <Link
                    data-tid="GoToLink"
                    href={`/AdminTools/FileData/DownloadBlob?blobId=${blobId}&blobSize=${blobSize}&isLargeBlob=${String(
                        isLargeBlob
                    )}`}>
                    {blobId}
                </Link>
            );
        }
    }

    return null;
}

function containsEntityIdWithProp(target: mixed, entityIdProp: string): boolean {
    return (
        typeof target === "object" &&
        typeof target.content === "object" &&
        typeof target.content.value === "object" &&
        typeof target.content.value.entityId === "object" &&
        typeof target.content.value.entityId[entityIdProp] === "string" &&
        target.content.value.entityId[entityIdProp]
    );
}

function getBusinessObjectLink(businessObjectName: string, id: string): JSX.Element {
    return (
        <Link
            data-tid="GoToLink"
            href={`/AdminTools/BusinessObjects/${businessObjectName}/details?ScopeId=${id}&Id=${id}`}>
            {id}
        </Link>
    );
}